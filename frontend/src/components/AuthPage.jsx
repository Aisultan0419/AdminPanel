import React, { useState } from 'react';
import { Container, Row, Col, Form, Button, InputGroup, Alert, Spinner } from 'react-bootstrap';
const getUniqIdValue = (prefix) => `${prefix}-${Math.random().toString(36).substr(2, 9)}`;
const AuthPage = ({ onAuthSuccess }) => {
  const [isLogin, setIsLogin] = useState(true);
  const [showPassword, setShowPassword] = useState(false);
  const [formData, setFormData] = useState({ name: '', email: '', password: '' });
  
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const nameInputId = getUniqIdValue('auth-name');
  const emailInputId = getUniqIdValue('auth-email');
  const passwordInputId = getUniqIdValue('auth-password');

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
    setError('');
    setSuccessMsg('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccessMsg('');
    setIsLoading(true);

  
    const endpoint = isLogin ? '/login' : '/register';
    const url = `https://localhost:7082/api/User${endpoint}`;

   
    const payload = isLogin 
      ? { 
          email: formData.email, 
          password: formData.password 
        }
      : { 
          email: formData.email, 
          plainPassword: formData.password, 
          name: formData.name 
        };

    try {
      const response = await fetch(url, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
      });

      const data = await response.json();

  
      if (!response.ok || !data.success) {
        setError(data.Message || 'An error occurred. Please try again.');
        return;
      }

      if (isLogin) {
        localStorage.setItem('jwt_token', data.data);
        if (onAuthSuccess) onAuthSuccess(data.data);
      } else {
        setSuccessMsg(data.Message || 'Registration successful! You can now sign in.');
        setFormData({ name: '', email: '', password: '' });
        setIsLogin(true);
      }

    } catch (err) {
      setError('Network error');
    } finally {
      setIsLoading(false);
    }
  };

  const toggleMode = () => {
    setIsLogin(!isLogin);
    setFormData({ name: '', email: '', password: '' });
    setError('');
    setSuccessMsg('');
  };


return (
  <Container fluid className="vh-100 p-0 m-0 overflow-hidden bg-light">
    <Row className="g-0 h-100">
      <Col md={7} lg={8} className="d-flex align-items-center justify-content-center p-4">
        <div className="w-100" style={{ maxWidth: '400px' }}>
          <h2 className="mb-4 text-center">
            {isLogin ? 'Sign In' : 'Sign Up'}
          </h2>

          {error && <Alert variant="danger" className="py-2 text-center rounded-1">{error}</Alert>}
          {successMsg && <Alert variant="success" className="py-2 text-center rounded-1">{successMsg}</Alert>}

          <Form onSubmit={handleSubmit}>
            {!isLogin && (
              <Form.Group className="mb-3" controlId="auth-name-field">
                <Form.Label>Name</Form.Label>
                <Form.Control
                  type="text"
                  name="name"
                  placeholder="Enter your name"
                  value={formData.name}
                  onChange={handleChange}
                  required
                  disabled={isLoading}
                />
              </Form.Group>
            )}

            <Form.Group className="mb-3" controlId="auth-email-field">
              <Form.Label>Email address</Form.Label>
              <Form.Control
                type="email"
                name="email"
                placeholder="Enter email"
                value={formData.email}
                onChange={handleChange}
                required
                disabled={isLoading}
              />
            </Form.Group>

            <Form.Group className="mb-4" controlId="auth-password-field">
              <Form.Label>Password</Form.Label>
              <InputGroup>
                <Form.Control
                  type={showPassword ? "text" : "password"}
                  name="password"
                  placeholder="Enter password"
                  value={formData.password}
                  onChange={handleChange}
                  required
                  disabled={isLoading}
                />
                <Button 
                  variant="link"
                  onClick={() => setShowPassword(!showPassword)}
                  style={{ 
                    border: '1px solid #dee2e6',
                    borderLeft: 0,
                    backgroundColor: '#fff',
                    color: '#6c757d',
                    textDecoration: 'none',
                    borderRadius: '0 0.375rem 0.375rem 0'
                  }}
                  className="d-flex align-items-center px-3"
                  disabled={isLoading}
                >
                  {showPassword ? (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"></path>
                      <circle cx="12" cy="12" r="3"></circle>
                    </svg>
                  ) : (
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24M1 1l22 22"></path>
                    </svg>
                  )}
                </Button>
              </InputGroup>
            </Form.Group>

            <Button variant="primary" type="submit" className="w-100 mb-3" disabled={isLoading}>
              {isLoading ? 'Processing...' : (isLogin ? 'Login' : 'Register')}
            </Button>
          </Form>

          <div className="text-center mt-2">
            <span className="text-muted">
              {isLogin ? "Not registered yet? " : "Already registered? "}
            </span>
            <span 
              className="text-primary" 
              style={{ cursor: 'pointer', textDecoration: 'underline' }} 
              onClick={!isLoading ? toggleMode : undefined}
            >
              {isLogin ? "Sign up" : "Sign in"}
            </span>
          </div>
        </div>
      </Col>

      <Col 
        md={5} 
        lg={4} 
        className="d-none d-md-block h-100" 
        style={{ 
          backgroundImage: 'url("https://images.unsplash.com/photo-1618005182384-a83a8bd57fbe?q=80&w=1964")',
          backgroundSize: 'cover', 
          backgroundPosition: 'center',
          backgroundRepeat: 'no-repeat'
        }}
      />
    </Row>
  </Container>
);
};

export default AuthPage;