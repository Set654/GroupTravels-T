"""Internal FastAPI application. Launch with uvicorn api:create_app --factory."""

import logging
import secrets

import requests
from fastapi import Depends, FastAPI, Query, Request, Response, Security
from fastapi.exceptions import RequestValidationError, ResponseValidationError
from fastapi.responses import JSONResponse
from fastapi.security import APIKeyHeader
from pydantic import ValidationError
from starlette.exceptions import HTTPException

from catalog import load_locations
from errors import (
    ConfigurationError, ProviderResponseError, ProviderSearchError,
    SearchValidationError, ServiceBusyError,
)
from schemas import ErrorResponse, FlightSearchResponse, HealthResponse
from service import FlightService
from settings import Settings

logger = logging.getLogger(__name__)
key_header = APIKeyHeader(name="X-Internal-Api-Key", auto_error=False)


def error(status: int, code: str, message: str) -> JSONResponse:
    return JSONResponse(status_code=status, content={"code": code, "message": message},
                        headers={"Cache-Control": "no-store"})


def create_app(settings: Settings | None = None, *, service: FlightService | None = None) -> FastAPI:
    settings = settings or Settings.from_env()
    settings.validate()
    load_locations()
    application = FastAPI(
        title="T-Travel internal flight search", version="1.0.0",
        description="SRS 3.5. Cached offers, not confirmed bookings. Internal service only.",
        redirect_slashes=False,
    )
    application.state.flight_service = service or FlightService(settings)

    def require_key(request: Request, provided: str | None = Security(key_header)) -> None:
        if (len(request.headers.getlist("X-Internal-Api-Key")) != 1
                or provided is None
                or not secrets.compare_digest(provided.encode("utf-8"), settings.internal_api_key.encode("utf-8"))):
            raise HTTPException(status_code=401)

    @application.exception_handler(RequestValidationError)
    async def invalid_request(request: Request, exc: RequestValidationError):
        return error(400, "INVALID_SEARCH_PARAMETERS", "Укажите origin, destination и departureDate в правильном формате.")

    @application.exception_handler(SearchValidationError)
    async def invalid_search(request: Request, exc: SearchValidationError):
        return error(400, "INVALID_SEARCH_PARAMETERS", str(exc))

    @application.exception_handler(HTTPException)
    async def http_error(request: Request, exc: HTTPException):
        if exc.status_code == 401:
            return error(401, "UNAUTHORIZED", "Неверный или отсутствующий ключ внутреннего сервиса.")
        if exc.status_code == 404:
            return error(404, "NOT_FOUND", "Маршрут не найден.")
        if exc.status_code == 405:
            response = error(405, "METHOD_NOT_ALLOWED", "Метод не поддерживается.")
            if exc.headers:
                response.headers.update(exc.headers)
            return response
        return error(exc.status_code, "HTTP_ERROR", "Запрос не выполнен.")

    @application.exception_handler(Exception)
    async def unexpected_error(request: Request, exc: Exception):
        # Do not log request headers, response bodies, URLs or exception text containing secrets.
        logger.error("Unhandled service exception: %s", type(exc).__name__)
        return error(500, "INTERNAL_ERROR", "Внутренняя ошибка сервиса.")

    @application.get("/health", response_model=HealthResponse, tags=["operations"])
    def health():
        return HealthResponse()

    responses = {status: {"model": ErrorResponse} for status in (400, 401, 500, 502, 503, 504)}

    @application.get(
        "/internal/flights/search", response_model=FlightSearchResponse,
        responses=responses, dependencies=[Depends(require_key)], tags=["flights"],
        operation_id="searchFlights",
    )
    def search_flights(
        request: Request,
        response: Response,
        origin: str = Query(..., min_length=1, max_length=16, examples=["MOW"]),
        destination: str = Query(..., min_length=1, max_length=16, examples=["LED"]),
        departureDate: str = Query(..., min_length=10, max_length=10, examples=["2026-12-15"]),
    ):
        allowed = {"origin", "destination", "departureDate"}
        if (set(request.query_params) - allowed
                or any(len(request.query_params.getlist(name)) != 1 for name in allowed)):
            raise SearchValidationError("Only one origin, destination and departureDate are supported")
        try:
            response.headers["Cache-Control"] = "no-store"
            result = application.state.flight_service.search(origin, destination, departureDate)
            return result
        except ConfigurationError:
            return error(503, "FLIGHT_SERVICE_UNAVAILABLE", "Авиапоиск не настроен.")
        except ServiceBusyError:
            response = error(503, "FLIGHT_SERVICE_UNAVAILABLE", "Авиапоиск занят. Повторите запрос позже.")
            response.headers["Retry-After"] = "1"
            return response
        except requests.exceptions.Timeout:
            return error(504, "FLIGHT_SEARCH_TIMEOUT", "Провайдер не ответил вовремя.")
        except (ProviderResponseError, ProviderSearchError, requests.exceptions.JSONDecodeError,
                requests.exceptions.HTTPError, ValidationError, ResponseValidationError):
            return error(502, "FLIGHT_PROVIDER_ERROR", "Не удалось получить корректные предложения провайдера.")
        except requests.exceptions.RequestException:
            return error(503, "FLIGHT_SERVICE_UNAVAILABLE", "Соединение с провайдером недоступно.")
        except SearchValidationError:
            raise
        except Exception as exc:
            logger.error("Unhandled search exception: %s", type(exc).__name__)
            return error(500, "INTERNAL_ERROR", "Внутренняя ошибка сервиса.")

    # Our validation handler returns 400, not FastAPI's default 422.
    default_openapi = application.openapi

    def openapi():
        schema = default_openapi()
        schema["paths"]["/internal/flights/search"]["get"]["responses"].pop("422", None)
        return schema

    application.openapi = openapi
    return application
