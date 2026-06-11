import React, { useState, useEffect } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css'; // Импорт стилей Bootstrap
import AuthPage from './components/AuthPage';   // Импорт нашей формы

function App() {
  // Состояние для хранения токена (проверяем LocalStorage при загрузке)
  const [token, setToken] = useState(localStorage.getItem('jwt_token'));

  // Функция для выхода (очистка токена)
  const handleLogout = () => {
    localStorage.removeItem('jwt_token');
    setToken(null);
  };

  return (
    <div className="app-container bg-light min-vh-100">
      {!token ? (
        // Если токена нет — показываем страницу логина/регистрации
        <AuthPage onAuthSuccess={(newToken) => setToken(newToken)} />
      ) : (
        // Если токен есть — временная заглушка будущей админки
        <div className="container p-5 text-center">
          <h1 className="mb-4">Admin Panel (User Management)</h1>
          <p className="text-muted">You are successfully authenticated!</p>
          <button className="btn btn-danger mt-3" onClick={handleLogout}>
            Logout
          </button>
        </div>
      )}
    </div>
  );
}

export default App;