import React, { useState } from 'react';
import 'bootstrap/dist/css/bootstrap.min.css';
import AuthPage from './components/AuthPage';
import AdminPanel from './components/AdminPanel'; 

function App() {
  const [token, setToken] = useState(localStorage.getItem('jwt_token'));

  const handleLogout = () => {
    localStorage.removeItem('jwt_token');
    setToken(null);
  };

  return (
    <div className="app-container bg-light min-vh-100">
      {!token ? (
        <AuthPage onAuthSuccess={(newToken) => setToken(newToken)} />
      ) : (
        <AdminPanel token={token} onLogout={handleLogout} />
      )}
    </div>
  );
}

export default App;