import unittest

from booking_links import build_booking_url
from errors import ProviderResponseError


class TestBookingLinks(unittest.TestCase):
    def test_relative_link_preserves_encoded_offer_query(self):
        self.assertEqual(build_booking_url("/search/MOW1512LED1?t=SU%2B123&x=1%26y%3D2"),
                         "https://www.aviasales.ru/search/MOW1512LED1?t=SU%2B123&x=1%26y%3D2")

    def test_allowed_absolute_urls(self):
        for host in ("aviasales.ru", "www.aviasales.ru", "aviasales.com", "www.aviasales.com"):
            with self.subTest(host=host):
                self.assertEqual(build_booking_url(f"https://{host}:443/search/example"),
                                 f"https://{host}/search/example")

    def test_absent_link_is_null(self):
        self.assertIsNone(build_booking_url(None))
        self.assertIsNone(build_booking_url(""))

    def test_external_internal_and_ambiguous_urls_are_rejected(self):
        invalid = [
            "http://www.aviasales.ru/search/example", "https://127.0.0.1/search/example",
            "https://localhost/search/example", "//www.aviasales.ru/search/example",
            "https://aviasales.ru.evil.example/search/example", "https://aviasales.ru@evil.example/search/example",
            "https://user@www.aviasales.ru/search/example", "https://www.aviasales.ru:444/search/example",
            "https://www.aviasales.ru/other", "/search/", "/search/../admin", "/search/%2e%2e/admin",
            "/search/%252e%252e/admin", "/search/%0d%0aheader", "/search/%5cevil", "/search/a#fragment",
            "/search/a\r\nheader", "/search/a\\evil", " /search/example", 123, [], {}, "/search/" + "a" * 16384,
        ]
        for value in invalid:
            with self.subTest(value_type=type(value).__name__):
                with self.assertRaises(ProviderResponseError):
                    build_booking_url(value)


if __name__ == "__main__":
    unittest.main()
