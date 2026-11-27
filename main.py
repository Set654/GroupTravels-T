"""Terminal entry point: python main.py --help."""

from app.cli import cli, get_cheap_bilets, save_tickets

__all__ = ["cli", "get_cheap_bilets", "save_tickets"]

if __name__ == "__main__":
    raise SystemExit(cli())
