import os
import requests
from dotenv import load_dotenv
load_dotenv()

TOKEN = os.getenv("TRAVELPAYOUTS_TOKEN")

def get_cheap_bilets(origin: str, destination: str, departure_at: str):
    url = 'https://api.travelpayouts.com/aviasales/v3/prices_for_dates'

    headers = {
        'X-Access-Token': TOKEN
    }

    params = {
        "destination": destination,
        "origin": origin,
        "departure_at": departure_at,
        "limit": 10,
        "currency": "rub",
        "oneway": "true"
    }

    response = requests.get(url, params=params, headers=headers)

    if response.status_code != 200:
        print(f'Ошибка: {response.status_code}')
        print (f'{response.text}')


    data = response.json()
    return data.get("data", [])

def print_tickets(tickets: list):

    if not tickets:
        print ("Билетов не найдено")
        return
    print(f"\n Найдено билетов: {len(tickets)}")

    for i, ticket in enumerate(tickets, 1):
        print(f'{i} {ticket.get("origin_airport")} -> {ticket.get("destination_airport")}')
        print(f"Цена: {ticket.get('price')}")
        print(f"Дата: {ticket.get('departure_at')}")
        print(f"Авиакомпания: {ticket.get("airline")}")
        print(f'Пересадки: {ticket.get("transfers")}')
        print('-' * 50)
if __name__ == "__main__":
    tickets = get_cheap_bilets(
        origin = "MOW",
        destination="LED",
        departure_at="2026-11"
    )
    print_tickets(tickets)
