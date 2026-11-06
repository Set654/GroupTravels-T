import json
import unittest
from datetime import datetime
from io import StringIO
from pathlib import Path
from tempfile import TemporaryDirectory
from unittest.mock import Mock, patch

import requests

import main
from errors import ConfigurationError, ProviderResponseError, ProviderSearchError, SearchValidationError


class TestFlights(unittest.TestCase):
    def setUp(self):
        token_patch = patch("main.TOKEN", "test-token")
        token_patch.start()
        self.addCleanup(token_patch.stop)

        self.response = Mock(spec=requests.Response)
        self.response.json.return_value = {"success": True, "data": []}
        request_patch = patch("main.requests.get", return_value=self.response)
        self.get = request_patch.start()
        self.addCleanup(request_patch.stop)

    def search(self, **kwargs):
        parameters = {"origin": "MOW", "destination": "LED", "departure_at": "2026-11"}
        parameters.update(kwargs)
        return main.get_cheap_bilets(**parameters)

    def test_empty_result(self):
        self.assertEqual(self.search(), [])

    def test_returns_found_tickets(self):
        expected_tickets = [
            {"price": 4500, "airline": "SU"},
            {"price": 7000, "airline": "S7"},
        ]
        self.response.json.return_value = {
            "success": True,
            "data": expected_tickets,
        }

        self.assertEqual(self.search(), expected_tickets)

    def test_unsuccessful_response(self):
        self.response.json.return_value = {
            "success": False,
            "data": [],
            "error": "Search failed",
        }

        with self.assertRaises(ProviderSearchError):
            self.search()

    def test_missing_token_prevents_request(self):
        for token in (None, ""):
            with self.subTest(token=token), patch("main.TOKEN", token):
                with self.assertRaisesRegex(ConfigurationError, "TRAVELPAYOUTS_TOKEN"):
                    self.search()

                self.get.assert_not_called()

    def test_timeout_is_propagated(self):
        error = requests.exceptions.Timeout("Test timeout")
        self.get.side_effect = error

        with self.assertRaises(requests.exceptions.Timeout) as caught:
            self.search()

        self.assertIs(caught.exception, error)

    def test_connection_error_is_propagated(self):
        error = requests.exceptions.ConnectionError("Test connection failure")
        self.get.side_effect = error

        with self.assertRaises(requests.exceptions.ConnectionError) as caught:
            self.search()

        self.assertIs(caught.exception, error)

    def test_http_errors_are_propagated_before_json_parsing(self):
        for status in (400, 401, 403, 429, 500, 503):
            with self.subTest(status=status):
                # Keep the real HTTP status check; only the network is replaced.
                response = requests.Response()
                response.status_code = status
                response.json = Mock(return_value={"success": True, "data": []})
                self.get.return_value = response

                with self.assertRaises(requests.exceptions.HTTPError) as caught:
                    self.search()

                self.assertIs(caught.exception.response, response)
                response.json.assert_not_called()

    def test_invalid_json_is_propagated(self):
        response = requests.Response()
        response.status_code = 200
        response._content = b"not valid JSON"
        self.get.return_value = response

        with self.assertRaises(requests.exceptions.JSONDecodeError):
            self.search()

    def test_response_must_be_dictionary(self):
        for payload in (None, [], "invalid", 123, True):
            with self.subTest(payload=payload):
                self.response.json.return_value = payload

                with self.assertRaisesRegex(ProviderResponseError, "dictionary"):
                    self.search()

    def test_missing_success_is_rejected(self):
        self.response.json.return_value = {"data": []}

        with self.assertRaises(ProviderSearchError):
            self.search()

    def test_success_must_be_boolean_true(self):
        for success in (None, 0, 1, "true", "false", [], {}):
            with self.subTest(success=success):
                self.response.json.return_value = {"success": success, "data": []}

                with self.assertRaises(ProviderSearchError):
                    self.search()

    def test_missing_tickets_are_rejected(self):
        self.response.json.return_value = {"success": True}

        with self.assertRaisesRegex(ProviderResponseError, "list"):
            self.search()

    def test_tickets_must_be_list(self):
        for tickets in (None, {}, "invalid", 123, False):
            with self.subTest(tickets=tickets):
                self.response.json.return_value = {"success": True, "data": tickets}

                with self.assertRaisesRegex(ProviderResponseError, "list"):
                    self.search()

    def test_every_ticket_must_be_dictionary(self):
        for tickets in ([123], [None], [[]], ["invalid"], [{"price": 4500}, 123]):
            with self.subTest(tickets=tickets):
                self.response.json.return_value = {"success": True, "data": tickets}

                with self.assertRaisesRegex(ProviderResponseError, "dictionary"):
                    self.search()

    def test_one_way_request_parameters(self):
        self.search()

        self.get.assert_called_once_with(
            "https://api.travelpayouts.com/aviasales/v3/prices_for_dates",
            params={
                "origin": "MOW",
                "destination": "LED",
                "departure_at": "2026-11",
                "limit": 10,
                "currency": "rub",
                "one_way": "true",
                "sorting": "price",
            },
            headers={"X-Access-Token": "test-token"},
            timeout=10,
        )

    def test_round_trip_request_parameters(self):
        self.search(return_at="2026-12")

        self.get.assert_called_once_with(
            "https://api.travelpayouts.com/aviasales/v3/prices_for_dates",
            params={
                "origin": "MOW",
                "destination": "LED",
                "departure_at": "2026-11",
                "return_at": "2026-12",
                "limit": 10,
                "currency": "rub",
                "one_way": "false",
                "sorting": "price",
            },
            headers={"X-Access-Token": "test-token"},
            timeout=10,
        )


    def test_invalid_codes_prevent_request(self):
        for name in ("origin", "destination"):
            for value in (None, 123, True, [], "", "   ", "MO", "MOWW", "МОВ", "M1W"):
                with self.subTest(parameter=name, value=value):
                    with self.assertRaisesRegex(SearchValidationError, name):
                        self.search(**{name: value})
                    self.get.assert_not_called()

    def test_codes_are_normalized(self):
        self.search(origin=" mow ", destination="led ")

        parameters = self.get.call_args.kwargs["params"]
        self.assertEqual(parameters["origin"], "MOW")
        self.assertEqual(parameters["destination"], "LED")

    def test_whitespace_token_prevents_request(self):
        with patch("main.TOKEN", " \t "):
            with self.assertRaisesRegex(ConfigurationError, "TRAVELPAYOUTS_TOKEN"):
                self.search()
        self.get.assert_not_called()

    def test_invalid_departure_dates_prevent_request(self):
        values = (
            None, 123, "", "202611", "2026-1", "2026-13", "2026-02-29",
            "2026-11-31", "2026-11-01T12:00:00", "２０２６-１１",
        )
        for value in values:
            with self.subTest(value=value):
                with self.assertRaisesRegex(SearchValidationError, "departure_at"):
                    self.search(departure_at=value)
                self.get.assert_not_called()

    def test_invalid_return_dates_prevent_request(self):
        for value in (123, False, "", "   ", "2026-13", "2026-02-29", "2026-1-01"):
            with self.subTest(value=value):
                with self.assertRaisesRegex(SearchValidationError, "return_at"):
                    self.search(return_at=value)
                self.get.assert_not_called()

    def test_return_period_before_departure_is_rejected(self):
        pairs = (
            ("2026-12", "2026-11"),
            ("2026-11-10", "2026-11-09"),
            ("2026-12", "2026-11-30"),
            ("2026-12-01", "2026-11"),
        )
        for departure_at, return_at in pairs:
            with self.subTest(departure_at=departure_at, return_at=return_at):
                with self.assertRaisesRegex(SearchValidationError, "return_at"):
                    self.search(departure_at=departure_at, return_at=return_at)
                self.get.assert_not_called()

    def test_overlapping_departure_and_return_periods_are_allowed(self):
        pairs = (
            ("2026-12", "2026-12"),
            ("2026-12-20", "2026-12"),
            ("2026-12", "2026-12-01"),
            ("2026-12-01", "2026-12-01"),
        )
        for departure_at, return_at in pairs:
            with self.subTest(departure_at=departure_at, return_at=return_at):
                self.assertEqual(self.search(departure_at=departure_at, return_at=return_at), [])
                parameters = self.get.call_args.kwargs["params"]
                self.assertEqual(parameters["departure_at"], departure_at)
                self.assertEqual(parameters["return_at"], return_at)

    def test_calendar_days_and_leap_month_are_supported(self):
        for departure_at in ("2026-11-06", "2028-02-29", "2028-02"):
            with self.subTest(departure_at=departure_at):
                self.assertEqual(self.search(departure_at=departure_at), [])
                self.assertEqual(
                    self.get.call_args.kwargs["params"]["departure_at"], departure_at
                )

    def test_date_whitespace_is_trimmed(self):
        self.search(departure_at=" 2026-11 ", return_at=" 2026-12 ")

        parameters = self.get.call_args.kwargs["params"]
        self.assertEqual(parameters["departure_at"], "2026-11")
        self.assertEqual(parameters["return_at"], "2026-12")


class TestSaveTickets(unittest.TestCase):
    def setUp(self):
        temporary_directory = TemporaryDirectory()
        self.addCleanup(temporary_directory.cleanup)
        self.directory = Path(temporary_directory.name)
        self.file_path = self.directory / "tickets.json"

        # Redirect only main's file operations; leave the working file untouched.
        open_patch = patch("main.open", side_effect=self.open_temporary_file, create=True)
        self.open = open_patch.start()
        self.addCleanup(open_patch.stop)

        self.now = datetime(2026, 11, 6, 12, 34, 56)
        time_patch = patch("main.datetime")
        clock = time_patch.start()
        self.addCleanup(time_patch.stop)
        clock.now.return_value = self.now

    def open_temporary_file(self, filename, mode, **kwargs):
        return (self.directory / filename).open(mode, **kwargs)

    def test_empty_tickets_do_not_create_file(self):
        main.save_tickets("MOW", "LED", "2026-11", [])

        self.open.assert_not_called()
        self.assertFalse(self.file_path.exists())

    def test_empty_tickets_preserve_existing_file(self):
        original = '{"tickets": [{"price": 1234}]}'
        self.file_path.write_text(original, encoding="utf-8")

        main.save_tickets("MOW", "LED", "2026-11", [])

        self.open.assert_not_called()
        self.assertEqual(self.file_path.read_text(encoding="utf-8"), original)

    def test_saves_tickets_and_search_metadata(self):
        tickets = [{"price": 4500, "airline": "SU", "label": "Прямой рейс"}]

        for return_at in (None, "2026-12"):
            with self.subTest(return_at=return_at):
                main.save_tickets("MOW", "LED", "2026-11", tickets, return_at=return_at)

                saved = json.loads(self.file_path.read_text(encoding="utf-8"))
                self.assertEqual(
                    saved,
                    {
                        "origin": "MOW",
                        "destination": "LED",
                        "departure_at": "2026-11",
                        "return_at": return_at,
                        "requested_at": "2026-11-06T12:34:56",
                        "tickets": tickets,
                    },
                )

    def test_new_results_replace_previous_file_contents(self):
        self.file_path.write_text(
            json.dumps({"old_metadata": "discard", "tickets": [{"price": 9000}]}),
            encoding="utf-8",
        )
        tickets = [{"price": 4500}]

        main.save_tickets("MOW", "LED", "2026-11", tickets)

        saved = json.loads(self.file_path.read_text(encoding="utf-8"))
        self.assertEqual(saved["tickets"], tickets)
        self.assertNotIn("old_metadata", saved)


    def test_custom_output_and_normalized_metadata(self):
        output = self.directory / "custom.json"
        main.save_tickets(
            " mow ", " led ", " 2026-11 ", [{"price": 4500}], filename=output
        )

        saved = json.loads(output.read_text(encoding="utf-8"))
        self.assertEqual(saved["origin"], "MOW")
        self.assertEqual(saved["destination"], "LED")
        self.assertEqual(saved["departure_at"], "2026-11")
        self.assertFalse(self.file_path.exists())


class TestCLI(unittest.TestCase):
    def setUp(self):
        search_patch = patch("main.get_cheap_bilets", return_value=[])
        self.search = search_patch.start()
        self.addCleanup(search_patch.stop)
        save_patch = patch("main.save_tickets")
        self.save = save_patch.start()
        self.addCleanup(save_patch.stop)
        self.arguments = [
            "--origin", "MOW", "--destination", "LED", "--departure-at", "2026-11",
        ]

    def test_prints_empty_result_without_saving_by_default(self):
        with patch("sys.stdout", new_callable=StringIO) as stdout:
            exit_code = main.cli(self.arguments)

        self.assertEqual(exit_code, 0)
        self.assertEqual(json.loads(stdout.getvalue()), [])
        self.search.assert_called_once_with("MOW", "LED", "2026-11", return_at=None)
        self.save.assert_not_called()

    def test_output_option_saves_search_results(self):
        tickets = [{"price": 4500}]
        self.search.return_value = tickets
        arguments = self.arguments + ["--return-at", "2026-12", "--output", "custom.json"]

        with patch("sys.stdout", new_callable=StringIO) as stdout:
            self.assertEqual(main.cli(arguments), 0)

        self.assertEqual(json.loads(stdout.getvalue()), tickets)
        self.search.assert_called_once_with("MOW", "LED", "2026-11", return_at="2026-12")
        self.save.assert_called_once_with(
            "MOW", "LED", "2026-11", tickets,
            return_at="2026-12", filename=Path("custom.json"),
        )

    def test_search_error_exits_with_failure_and_does_not_save(self):
        self.search.side_effect = RuntimeError("TRAVELPAYOUTS_TOKEN is not configured")

        with patch("sys.stderr", new_callable=StringIO) as stderr:
            with self.assertRaises(SystemExit) as caught:
                main.cli(self.arguments)

        self.assertEqual(caught.exception.code, 1)
        self.assertIn("TRAVELPAYOUTS_TOKEN", stderr.getvalue())
        self.save.assert_not_called()

    def test_missing_arguments_do_not_start_search(self):
        with patch("sys.stderr", new_callable=StringIO):
            with self.assertRaises(SystemExit) as caught:
                main.cli([])

        self.assertEqual(caught.exception.code, 2)
        self.search.assert_not_called()
        self.save.assert_not_called()

    def test_help_does_not_start_search(self):
        with patch("sys.stdout", new_callable=StringIO) as stdout:
            with self.assertRaises(SystemExit) as caught:
                main.cli(["--help"])

        self.assertEqual(caught.exception.code, 0)
        self.assertIn("--departure-at", stdout.getvalue())
        self.search.assert_not_called()
        self.save.assert_not_called()


if __name__ == "__main__":
    unittest.main()
