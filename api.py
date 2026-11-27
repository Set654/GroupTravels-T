"""HTTP entry point: python -m uvicorn api:create_app --factory."""

from app.api import create_app

__all__ = ["create_app"]
