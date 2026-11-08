import json
import unittest
from datetime import date
from pathlib import Path
from unittest.mock import Mock, patch

import requests
from fastapi.testclient import TestClient
from pydantic import ValidationError

from api import create_app
from service import FlightService
from settings import Settings
from schemas import FlightOffer

ROOT = Path(__file__).parent


class TestExportedContract(unittest.TestCase):
    def setUp(self):
        self.settings = Settings(travelpayouts_token="contract-test-token",
                                 internal_api_key="contract-test-key-" + "b" * 32)
        service = FlightService(self.settings, today=lambda: date(2026, 11, 7))
        self.app = create_app(self.settings, service=service)

    def test_exported_openapi_matches_running_application(self):
        exported = json.loads((ROOT / "docs" / "openapi.json").read_text(encoding="utf-8"))
        self.assertEqual(self.app.openapi(), exported)

    def test_offer_requires_every_key_even_when_its_value_can_be_null(self):
        response = json.loads((ROOT / "examples/responses/search-success.json").read_text(encoding="utf-8"))
        nullable_offer = response["flights"][0]
        self.assertIsNone(FlightOffer.model_validate(nullable_offer).bookingUrl)
        for name in nullable_offer:
            with self.subTest(missing=name):
                incomplete = {key: value for key, value in nullable_offer.items() if key != name}
                with self.assertRaises(ValidationError):
                    FlightOffer.model_validate(incomplete)

    def test_exported_success_and_empty_examples_match_http_behavior(self):
        tickets = json.loads((ROOT / "examples" / "provider_offers.json").read_text(encoding="utf-8"))
        response = Mock(spec=requests.Response)
        response.status_code = 200
        with TestClient(self.app) as client, patch("travelpayouts.requests.get", return_value=response):
            for payload, filename in ((tickets, "search-success.json"), ([], "search-empty.json")):
                with self.subTest(filename=filename):
                    response.json.return_value = {"success": True, "data": payload}
                    actual = client.get(
                        "/internal/flights/search",
                        params={"origin": "MOW", "destination": "LED", "departureDate": "2026-12-15"},
                        headers={"X-Internal-Api-Key": self.settings.internal_api_key},
                    )
                    expected = json.loads((ROOT / "examples" / "responses" / filename).read_text(encoding="utf-8"))
                    self.assertEqual(actual.status_code, 200, actual.text)
                    self.assertEqual(actual.json(), expected)


if __name__ == "__main__":
    unittest.main()
