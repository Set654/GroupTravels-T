"""Client errors that a future HTTP handler can distinguish by type."""


class SearchValidationError(ValueError):
    """Search arguments have an invalid type, format, or date order."""


class ConfigurationError(RuntimeError):
    """The Travelpayouts token is missing or empty."""


class ProviderResponseError(ValueError):
    """Travelpayouts returned an unexpected JSON structure."""


class ProviderSearchError(RuntimeError):
    """Travelpayouts did not confirm a successful search."""
