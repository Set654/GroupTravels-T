import copy
import json
import unittest

from errors import ProviderResponseError
from normalization import normalize_flight


class TestNormalizeFlight(unittest.TestCase):
    def setUp(self):
        self.ticket = {
            "origin": "MOW",
            "destination": "LED",
            "price": 4500,
            "airline": "SU",
            "flight_number": "0123A",
        }

    def test_normalizes_offer_and_preserves_flight_number_text(self):
        self.assertEqual(
            normalize_flight(self.ticket),
            {
                "origin": "MOW",
                "destination": "LED",
                "price": 4500,
                "airline": "SU",
                "flightNumber": "0123A",
            },
        )

    def test_does_not_modify_original_offer(self):
        self.ticket.update(origin=" mow ", airline=" su ", extra={"values": [1]})
        original = copy.deepcopy(self.ticket)

        result = normalize_flight(self.ticket)

        self.assertEqual(self.ticket, original)
        self.assertIsNot(result, self.ticket)
        self.assertNotIn("extra", result)

    def test_normalizes_whitespace_and_code_case(self):
        self.ticket.update(
            origin=" mow ", destination=" led ", airline=" su ", flight_number=" 0123A "
        )

        result = normalize_flight(self.ticket)

        self.assertEqual(result["origin"], "MOW")
        self.assertEqual(result["destination"], "LED")
        self.assertEqual(result["airline"], "SU")
        self.assertEqual(result["flightNumber"], "0123A")

    def test_missing_null_and_blank_optional_fields_become_json_null(self):
        for optional in ({}, {"airline": None, "flight_number": None},
                         {"airline": " ", "flight_number": ""}):
            with self.subTest(optional=optional):
                ticket = {"origin": "MOW", "destination": "LED", "price": 4500}
                result = normalize_flight({**ticket, **optional})

                self.assertIsNone(result["airline"])
                self.assertIsNone(result["flightNumber"])
                encoded = json.dumps(result, allow_nan=False)
                self.assertIsNone(json.loads(encoded)["flightNumber"])

    def test_numeric_flight_number_becomes_string(self):
        self.ticket["flight_number"] = 1234

        self.assertEqual(normalize_flight(self.ticket)["flightNumber"], "1234")

    def test_invalid_offer_type_is_rejected(self):
        for ticket in (None, [], "invalid", 123, True):
            with self.subTest(ticket=ticket):
                with self.assertRaisesRegex(ProviderResponseError, "dictionary"):
                    normalize_flight(ticket)

    def test_missing_and_invalid_route_codes_are_rejected(self):
        for name in ("origin", "destination"):
            ticket = self.ticket.copy()
            del ticket[name]
            with self.subTest(name=name, missing=True):
                with self.assertRaisesRegex(ProviderResponseError, name):
                    normalize_flight(ticket)
            for value in (None, "", " ", "RU", "MO1", "MOSCOW", 123, [], True):
                with self.subTest(name=name, value=value):
                    ticket[name] = value
                    with self.assertRaisesRegex(ProviderResponseError, name):
                        normalize_flight(ticket)

    def test_missing_or_invalid_price_is_rejected(self):
        ticket = self.ticket.copy()
        del ticket["price"]
        with self.assertRaisesRegex(ProviderResponseError, "price"):
            normalize_flight(ticket)
        for price in (None, "4500", True, False, 0, -1, -0.5, [], {},
                      float("nan"), float("inf"), float("-inf")):
            with self.subTest(price=price):
                ticket["price"] = price
                with self.assertRaisesRegex(ProviderResponseError, "price"):
                    normalize_flight(ticket)

    def test_price_is_not_rounded_or_converted_to_text(self):
        for price in (4500, 4500.75):
            with self.subTest(price=price):
                self.ticket["price"] = price
                result = normalize_flight(self.ticket)

                self.assertEqual(result["price"], price)
                self.assertIs(type(result["price"]), type(price))

    def test_invalid_optional_field_types_are_rejected(self):
        for name, values in (
            ("airline", (123, True, [], {})),
            ("flight_number", (True, False, 12.5, 0, -1, [], {})),
        ):
            for value in values:
                with self.subTest(name=name, value=value):
                    ticket = {**self.ticket, name: value}
                    with self.assertRaisesRegex(ProviderResponseError, name):
                        normalize_flight(ticket)


if __name__ == "__main__":
    unittest.main()
