import { useState } from 'react';
import './DashboardPage.css';

// Пока сервер не подключён, данные о поездках хранятся в массиве.
const trips = [
  { id: 1, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 2, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 3, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 4, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 5, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 6, name: 'Название поездки', dates: '21–30 ноября 2026', people: 4, expenses: 24000, type: 'active' },
  { id: 7, name: 'Поездка в Казань', dates: '15–20 декабря 2026', people: 3, expenses: 8500, type: 'future' },
  { id: 8, name: 'Зимние каникулы', dates: '3–10 января 2027', people: 5, expenses: 0, type: 'future' },
  { id: 9, name: 'Отдых на море', dates: '10–20 августа 2026', people: 4, expenses: 42000, type: 'past' },
];

const filters = [
  { id: 'active', label: 'Активные', heading: 'Активные поездки' },
  { id: 'future', label: 'Предстоящие', heading: 'Предстоящие поездки' },
  { id: 'past', label: 'Прошедшие', heading: 'Прошедшие поездки' },
];

const statusLabels = {
  active: 'Активная',
  future: 'Предстоящая',
  past: 'Прошедшая',
};

function TripCard({ trip }) {
  return (
    <button
      className="trip-card"
      type="button"
      onClick={() => window.alert('Экран отдельной поездки добавим на следующем этапе.')}
    >
      <div className="trip-card-top">
        <span className="trip-icon">Иконка</span>
        <span className="trip-status">{statusLabels[trip.type]}</span>
      </div>

      <div className="trip-card-bottom">
        <h3>{trip.name}</h3>
        <div className="trip-meta">
          <span className="meta-icon">▦</span>
          <span>{trip.dates}</span>
        </div>
        <div className="trip-meta">
          <span className="meta-icon">♙</span>
          <span>{trip.people} участника</span>
        </div>
        <div className="trip-expenses">
          <span>Расходы:</span>
          <strong>{trip.expenses.toLocaleString('ru-RU')} ₽</strong>
        </div>
      </div>
    </button>
  );
}

function BalancePanel() {
  return (
    <section className="balance-panel">
      <h2>Баланс</h2>
      <p className="balance-caption">Общий баланс по всем поездкам</p>

      <div className="balance-item">
        <span>Мне должны всего:</span>
        <strong>5000 ₽</strong>
      </div>
      <div className="balance-item">
        <span>Я должен всего:</span>
        <strong>3500 ₽</strong>
      </div>
      <div className="balance-item">
        <span>Чистый баланс:</span>
        <strong>1500 ₽</strong>
      </div>

      <button
        className="details-button"
        type="button"
        onClick={() => window.alert('Подробный баланс добавим позже.')}
      >
        Подробнее
      </button>
    </section>
  );
}

export default function App() {
  const [activeFilter, setActiveFilter] = useState('active');
  const [darkTheme, setDarkTheme] = useState(false);

  const visibleTrips = trips.filter((trip) => trip.type === activeFilter);
  const currentFilter = filters.find((filter) => filter.id === activeFilter);
  const tripWord = visibleTrips.length === 1 ? 'поездка' :
    visibleTrips.length >= 2 && visibleTrips.length <= 4 ? 'поездки' : 'поездок';
  const userName = sessionStorage.getItem('ttravel_user_name') || 'Пользователь';
  return (
    <div className={`app ${darkTheme ? 'dark' : ''}`}>
      <header className="header">
        <div className="header-content">
          <span className="brand">Т-путешествия</span>
          <div className="header-user">
            <button
              className={`theme-switch ${darkTheme ? 'theme-switch-on' : ''}`}
              type="button"
              aria-label="Переключить тему"
              aria-pressed={darkTheme}
              onClick={() => setDarkTheme(!darkTheme)}
            >
              <span className="theme-switch-circle">☀</span>
            </button>
            <span className="username">{userName}</span>
            <span className="user-avatar">{userName.charAt(0).toUpperCase()}</span>
          </div>
        </div>
      </header>

      <main className="dashboard">
        <section className="trips-section">
          <h1>Поездки</h1>

          <div className="filters" aria-label="Категории поездок">
            {filters.map((filter) => (
              <button
                key={filter.id}
                className={`filter-button ${activeFilter === filter.id ? 'selected' : ''}`}
                type="button"
                aria-pressed={activeFilter === filter.id}
                onClick={() => setActiveFilter(filter.id)}
              >
                {filter.label}
              </button>
            ))}
          </div>

          <div className="trips-title-row">
            <h2>{currentFilter.heading}</h2>
            <span>{visibleTrips.length} {tripWord}</span>
          </div>

          {visibleTrips.length > 0 ? (
            <div className="trips-grid">
              {visibleTrips.map((trip) => (
                <TripCard key={trip.id} trip={trip} />
              ))}
            </div>
          ) : (
            <p className="empty-message">В этой категории поездок пока нет.</p>
          )}
        </section>

        <aside className="sidebar">
          <button
            className="create-button"
            type="button"
            onClick={() => window.alert('Форму создания поездки добавим на следующем этапе.')}
          >
            Создать новую поездку
          </button>
          <BalancePanel />
        </aside>
      </main>
    </div>
  );
}
