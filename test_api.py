import json
import unittest
from dataclasses import replace
from datetime import date
from unittest.mock import Mock, patch

import requests
from urllib3.exceptions import ReadTimeoutError
from fastapi.testclient import TestClient

from api import create_app
from catalog import load_locations
from errors import ConfigurationError
from service import FlightService
from settings import Settings


class TestFlightAPI(unittest.TestCase):
    def setUp(self):
        self.settings = Settings(travelpayouts_token="provider-test-token",
                                 internal_api_key="internal-test-key-" + "a" * 32)
        self.service = FlightService(self.settings, today=lambda: date(2026, 11, 7))
        self.app = create_app(self.settings, service=self.service)
        self.client = self.enterContext(TestClient(self.app))
        self.response = Mock(spec=requests.Response)
        self.response.status_code = 200
        self.response.json.return_value = {"success": True, "data": []}
        self.get = self.enterContext(patch("travelpayouts.requests.get", return_value=self.response))
        self.headers = {"X-Internal-Api-Key": self.settings.internal_api_key}
        self.query = {"origin": "MOW", "destination": "LED", "departureDate": "2026-12-15"}
        self.ticket = {
            "origin": "MOW", "destination": "LED", "origin_airport": "SVO",
            "destination_airport": "LED", "price": 4500.75, "airline": "SU",
            "flight_number": "0123A", "departure_at": "2026-12-15T10:00:00+03:00",
            "duration_to": 95, "duration": 200, "transfers": 0,
            "link": "/search/MOW1512LED1?t=example&expected_price_currency=rub",
        }

    def search(self, **changes):
        return self.client.get("/internal/flights/search", params={**self.query, **changes},
                               headers=self.headers)

    def assert_error(self, response, status, code):
        self.assertEqual(response.status_code, status, response.text)
        self.assertEqual(set(response.json()), {"code", "message"})
        self.assertEqual(response.json()["code"], code)
        self.assertNotIn(self.settings.travelpayouts_token, response.text)
        self.assertNotIn(self.settings.internal_api_key, response.text)

    def test_health_is_local_liveness_without_provider_calls(self):
        response = self.client.get("/health")
        self.assertEqual(response.json(), {"status": "ok"})
        self.get.assert_not_called()

    def test_missing_wrong_and_duplicate_keys_are_rejected_before_search(self):
        for headers in ({}, {"X-Internal-Api-Key": "wrong"},
                        [("X-Internal-Api-Key", self.settings.internal_api_key)] * 2):
            with self.subTest(headers_type=type(headers).__name__):
                response = self.client.get("/internal/flights/search", params=self.query, headers=headers)
                self.assert_error(response, 401, "UNAUTHORIZED")
        self.get.assert_not_called()

    def test_empty_search_returns_empty_flights(self):
        response = self.search()
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json(), {
            "source": "Aviasales", "dataType": "cached_offers", "currency": "RUB", "flights": [],
        })
        self.assertEqual(response.headers["Cache-Control"], "no-store")

    def test_full_response_is_sorted_and_uses_outbound_duration(self):
        self.response.json.return_value["data"] = [
            {**self.ticket, "price": 7000}, self.ticket,
        ]
        response = self.search(origin=" mow ", destination="led")
        self.assertEqual(response.status_code, 200, response.text)
        flights = response.json()["flights"]
        self.assertEqual([flight["price"] for flight in flights], [4500.75, 7000])
        self.assertEqual(flights[0], {
            "origin": "MOW", "destination": "LED", "departureDate": "2026-12-15",
            "departureAt": "2026-12-15T07:00:00Z", "airline": "SU", "flightNumber": "0123A",
            "price": 4500.75, "currency": "RUB", "transfers": 0, "durationMinutes": 95,
            "bookingUrl": "https://www.aviasales.ru/search/MOW1512LED1?t=example&expected_price_currency=rub",
        })
        self.assertNotIn("offerId", flights[0])
        self.get.assert_called_once_with(
            "https://api.travelpayouts.com/aviasales/v3/prices_for_dates",
            params={"origin": "MOW", "destination": "LED", "departure_at": "2026-12-15",
                    "limit": 10, "currency": "rub", "one_way": "true", "sorting": "price"},
            headers={"X-Access-Token": "provider-test-token"}, timeout=(3.0, 10.0), allow_redirects=False,
        )
        self.response.close.assert_called_once()

    def test_missing_optional_fields_are_explicit_null(self):
        self.response.json.return_value["data"] = [{"origin": "MOW", "destination": "LED", "price": 1}]
        flight = self.search().json()["flights"][0]
        for name in ("departureAt", "airline", "flightNumber", "transfers", "durationMinutes", "bookingUrl"):
            with self.subTest(field=name):
                self.assertIsNone(flight[name])
        self.assertEqual(flight["departureDate"], "2026-12-15")

    def test_naive_time_is_not_assigned_an_invented_offset(self):
        for departure in ("2026-12-15T10:00:00", "2026-12-15"):
            with self.subTest(departure=departure):
                self.response.json.return_value["data"] = [{**self.ticket, "departure_at": departure}]
                response = self.search()
                self.assertEqual(response.status_code, 200)
                self.assertIsNone(response.json()["flights"][0]["departureAt"])

    def test_local_departure_date_survives_utc_day_change(self):
        self.response.json.return_value["data"] = [{**self.ticket, "departure_at": "2026-12-15T01:00:00+13:00"}]
        flight = self.search().json()["flights"][0]
        self.assertEqual(flight["departureDate"], "2026-12-15")
        self.assertEqual(flight["departureAt"], "2026-12-14T12:00:00Z")

    def test_one_way_duration_fallback(self):
        ticket = {**self.ticket, "duration": 95}
        del ticket["duration_to"]
        self.response.json.return_value["data"] = [ticket]
        self.assertEqual(self.search().json()["flights"][0]["durationMinutes"], 95)

    def test_airport_request_can_match_provider_city_with_correct_airport(self):
        self.response.json.return_value["data"] = [self.ticket]
        response = self.search(origin="SVO")
        self.assertEqual(response.status_code, 200, response.text)
        self.assertEqual(response.json()["flights"][0]["origin"], "SVO")

    def test_city_request_can_match_airport_response(self):
        self.response.json.return_value["data"] = [{**self.ticket, "origin": "SVO"}]
        self.assertEqual(self.search().status_code, 200)

    def test_wrong_airport_for_specific_request_is_provider_error(self):
        self.response.json.return_value["data"] = [{**self.ticket, "origin_airport": "DME"}]
        self.assert_error(self.search(origin="SVO"), 502, "FLIGHT_PROVIDER_ERROR")

    def test_missing_parameters_are_400(self):
        for name in self.query:
            with self.subTest(name=name):
                query = {k: v for k, v in self.query.items() if k != name}
                response = self.client.get("/internal/flights/search", params=query, headers=self.headers)
                self.assert_error(response, 400, "INVALID_SEARCH_PARAMETERS")
        self.get.assert_not_called()

    def test_invalid_parameters_never_call_provider(self):
        self.assertNotIn("ZZZ", load_locations())
        cases = [
            {"origin": "ZZZ"}, {"destination": "ZZZ"}, {"origin": "МОВ"},
            {"origin": "M1W"}, {"origin": ""}, {"origin": "MOWW"}, {"destination": "MOW"},
            {"departureDate": "2026-12"}, {"departureDate": "2026-02-30"},
            {"departureDate": "2026-11-06"}, {"departureDate": "2026-12-15T00:00:00Z"},
            {"departureDate": "２０２６-１２-１５"},
        ]
        for changes in cases:
            with self.subTest(changes=changes):
                self.assert_error(self.search(**changes), 400, "INVALID_SEARCH_PARAMETERS")
        self.get.assert_not_called()

    def test_today_is_allowed(self):
        self.assertEqual(self.search(departureDate="2026-11-07").status_code, 200)

    def test_unknown_and_duplicate_query_parameters_are_400(self):
        cases = [{**self.query, "return_at": "2026-12-20"}, {**self.query, "tripId": "1"},
                 [*self.query.items(), ("origin", "SVO")]]
        for query in cases:
            with self.subTest(query=query):
                response = self.client.get("/internal/flights/search", params=query, headers=self.headers)
                self.assert_error(response, 400, "INVALID_SEARCH_PARAMETERS")
        self.get.assert_not_called()

    def test_provider_timeout_is_504_without_exception_text(self):
        self.get.side_effect = requests.exceptions.Timeout(self.settings.travelpayouts_token)
        self.assert_error(self.search(), 504, "FLIGHT_SEARCH_TIMEOUT")

    def test_provider_connection_failure_is_503(self):
        self.get.side_effect = requests.exceptions.ConnectionError("private provider details")
        self.assert_error(self.search(), 503, "FLIGHT_SERVICE_UNAVAILABLE")

    def test_provider_body_read_timeout_wrapped_as_connection_error_is_504(self):
        self.get.side_effect = requests.exceptions.ConnectionError(
            ReadTimeoutError(None, None, "response body timed out")
        )
        self.assert_error(self.search(), 504, "FLIGHT_SEARCH_TIMEOUT")

    def test_provider_http_errors_are_502(self):
        for status in (400, 401, 403, 429, 500, 503):
            with self.subTest(status=status):
                self.response.raise_for_status.side_effect = requests.exceptions.HTTPError(str(status))
                self.assert_error(self.search(), 502, "FLIGHT_PROVIDER_ERROR")
        self.response.json.assert_not_called()

    def test_provider_redirect_is_not_followed(self):
        self.response.status_code = 302
        self.assert_error(self.search(), 502, "FLIGHT_PROVIDER_ERROR")
        self.response.json.assert_not_called()

    def test_invalid_provider_json_is_502(self):
        self.response.json.side_effect = requests.exceptions.JSONDecodeError("invalid", "<html>", 0)
        self.assert_error(self.search(), 502, "FLIGHT_PROVIDER_ERROR")

    def test_invalid_provider_envelopes_are_502(self):
        cases = [None, [], {"success": False, "data": []}, {"success": 1, "data": []},
                 {"success": True}, {"success": True, "data": {}},
                 {"success": True, "data": [None]},
                 {"success": True, "data": [], "currency": "USD"},
                 {"success": True, "data": [self.ticket] * 11}]
        for payload in cases:
            with self.subTest(payload_type=type(payload).__name__):
                self.response.json.return_value = payload
                self.assert_error(self.search(), 502, "FLIGHT_PROVIDER_ERROR")

    def test_corrupt_provider_rows_fail_without_partial_results(self):
        cases = [
            {"price": 0}, {"price": True}, {"price": "4500"}, {"price": float("nan")},
            {"price": 4500.123}, {"price": 1e-100}, {"price": 79228162514264337593543950336},
            {"origin": "AER"}, {"destination": "AER"}, {"origin_airport": "LED"},
            {"departure_at": "2026-12-16T10:00:00+03:00"}, {"departure_at": "not a date"},
            {"transfers": -1}, {"transfers": True}, {"duration_to": -5}, {"duration_to": "95"},
            {"currency": "USD"}, {"return_at": "2026-12-20T10:00:00Z"}, {"duration_back": 95},
            {"link": "http://127.0.0.1/secrets"},
        ]
        for changes in cases:
            with self.subTest(changes=changes):
                self.response.json.return_value = {"success": True, "data": [self.ticket, {**self.ticket, **changes}]}
                self.assert_error(self.search(), 502, "FLIGHT_PROVIDER_ERROR")

    def test_saturated_service_is_503_without_provider_call(self):
        for _ in range(self.settings.max_concurrent_searches):
            self.service.slots.acquire()
            self.addCleanup(self.service.slots.release)
        response = self.search()
        self.assert_error(response, 503, "FLIGHT_SERVICE_UNAVAILABLE")
        self.assertEqual(response.headers["Retry-After"], "1")
        self.get.assert_not_called()

    def test_request_slot_is_released_after_failure(self):
        self.get.side_effect = requests.exceptions.Timeout("test")
        self.assertEqual(self.search().status_code, 504)
        self.get.side_effect = None
        self.assertEqual(self.search().status_code, 200)

    def test_unexpected_error_is_sanitized_and_logged_without_secrets(self):
        self.get.side_effect = RuntimeError(self.settings.travelpayouts_token)
        with self.assertLogs("api", level="ERROR") as logs:
            self.assert_error(self.search(), 500, "INTERNAL_ERROR")
        self.assertNotIn(self.settings.travelpayouts_token, " ".join(logs.output))

    def test_missing_token_or_internal_key_prevents_startup(self):
        for changes in ({"travelpayouts_token": ""}, {"internal_api_key": ""}, {"internal_api_key": "short"}):
            with self.subTest(changes=changes):
                with self.assertRaises(ConfigurationError):
                    create_app(replace(self.settings, **changes))

    def test_invalid_settings_are_rejected_and_repr_hides_keys(self):
        cases = [{"connect_timeout": 0}, {"read_timeout": float("nan")},
                 {"read_timeout": 61}, {"max_concurrent_searches": 0},
                 {"max_concurrent_searches": 33}, {"search_timezone": "Invalid/Zone"},
                 {"internal_api_key": "я" * 32},
                 {"internal_api_key": " " * 32}, {"internal_api_key": "a" * 32 + "\n"},
                 {"travelpayouts_token": self.settings.internal_api_key}]
        for changes in cases:
            with self.subTest(changes=changes):
                with self.assertRaises(ConfigurationError):
                    replace(self.settings, **changes).validate()
        self.assertNotIn(self.settings.travelpayouts_token, repr(self.settings))
        self.assertNotIn(self.settings.internal_api_key, repr(self.settings))

    def test_openapi_describes_actual_security_responses_and_nullability(self):
        schema = self.client.get("/openapi.json").json()
        operation = schema["paths"]["/internal/flights/search"]["get"]
        self.assertNotIn("422", operation["responses"])
        self.assertEqual(set(operation["responses"]), {"200", "400", "401", "500", "502", "503", "504"})
        self.assertTrue(operation["security"])
        self.assertEqual(schema["components"]["securitySchemes"]["APIKeyHeader"]["name"], "X-Internal-Api-Key")
        self.assertNotIn("offerId", schema["components"]["schemas"]["FlightOffer"]["properties"])
        offer_schema = schema["components"]["schemas"]["FlightOffer"]
        self.assertEqual(set(offer_schema["required"]), set(offer_schema["properties"]))
        self.assertNotIn(self.settings.internal_api_key, json.dumps(schema))

    def test_unknown_route_and_wrong_method_have_error_envelope(self):
        self.assert_error(self.client.get("/missing"), 404, "NOT_FOUND")
        self.assert_error(self.client.post("/internal/flights/search", headers=self.headers), 405, "METHOD_NOT_ALLOWED")


if __name__ == "__main__":
    unittest.main()
