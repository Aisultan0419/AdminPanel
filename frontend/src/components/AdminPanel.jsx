import React, { useState, useEffect } from 'react';
import { Container, Table, Button, Form, Alert, Modal, OverlayTrigger, Tooltip } from 'react-bootstrap';
import { API_BASE_URL } from '../config';

const parseJwt = (token) => {
  try {
    return JSON.parse(atob(token.split('.')[1]));
  } catch (e) {
    return null;
  }
};

const AdminPanel = ({ token, onLogout }) => {
  const [users, setUsers] = useState([]);
  const [selectedIds, setSelectedIds] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [showVerifyModal, setShowVerifyModal] = useState(false);
  const [verificationCode, setVerificationCode] = useState('');
  const [timeLeft, setTimeLeft] = useState(300); 
  const [verifyError, setVerifyError] = useState('');
  const [isVerifying, setIsVerifying] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');

  useEffect(() => {
    fetchUsers();
  }, []);

  useEffect(() => {
    let timer;
    if (showVerifyModal && timeLeft > 0) {
      timer = setInterval(() => {
        setTimeLeft((prevTime) => prevTime - 1);
      }, 1000);
    }
    return () => clearInterval(timer);
  }, [showVerifyModal, timeLeft]);

  useEffect(() => {
    if (successMsg) {
      const timer = setTimeout(() => {
        setSuccessMsg('');
      }, 3000); 
      return () => clearTimeout(timer);
    }
  }, [successMsg]);

  const formatTime = (seconds) => {
    const m = Math.floor(seconds / 60);
    const s = seconds % 60;
    return `${m}:${s < 10 ? '0' : ''}${s}`;
  };

  const decodedToken = parseJwt(token);
  const currentUserId = decodedToken ? (decodedToken["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || decodedToken.id || decodedToken.sub) : null;

  const checkSelfAction = async (ids, action) => {
    const currentUser = users.find(u => u.id === currentUserId);
    if (action === 'delete-unverified') {
      if (currentUser && currentUser.status !== 'Active') {
        return; 
      }
      await fetchUsers();
      return;
    }
    const affectedMyself = ids && ids.includes(currentUserId);
    const isCriticalAction = action === 'block' || action === 'delete';
    if (!affectedMyself || !isCriticalAction) {
      await fetchUsers();
    }
  };

  const handleSendVerification = async () => {
    const currentUser = users.find(u => u.id === currentUserId);
    const email = currentUser?.email || decodedToken?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decodedToken?.email;

    if (!email) {
      setError('Unable to find your email address.');
      return;
    }

    setIsVerifying(true);
    setError('');

    try {
      const response = await fetch(`${API_BASE_URL}/send-verification?clientEmail=${encodeURIComponent(email)}`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (await handleApiResponse(response.clone(), setError, setSuccessMsg)) return;

      setTimeLeft(300);
      setVerificationCode('');
      setVerifyError('');
      setShowVerifyModal(true);
    } catch (err) {
      setError('Failed to send verification code. Please check your connection.');
    } finally {
      setIsVerifying(false);
    }
  };

  const handleVerifyCode = async () => {
    if (verificationCode.length !== 6) {
      setVerifyError('Code must be exactly 6 digits.');
      return;
    }

    const currentUser = users.find(u => u.id === currentUserId);
    const email = currentUser?.email || decodedToken?.["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] || decodedToken?.email;

    setIsVerifying(true);
    setVerifyError('');

    try {
      const response = await fetch(`${API_BASE_URL}/verify?clientEmail=${encodeURIComponent(email)}&code=${verificationCode}`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` }
      });

      if (await handleApiResponse(response.clone(), setVerifyError, setSuccessMsg)) return;
      setVerifyError(''); 
      setShowVerifyModal(false);
      await fetchUsers();
    } catch (err) {
      setVerifyError('Failed to verify code. Please check your connection.');
    } finally {
      setIsVerifying(false);
    }
  };

  const deleteUnverifiedUsers = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/delete-unverified`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (await handleApiResponse(response.clone(), setError, setSuccessMsg)) return;
      const result = await response.json();

      await checkSelfAction(null, 'delete-unverified');

    } catch (err) {
      setError('Connection lost. Failed to delete unverified users. Please try again.');
    }
  };

  const deleteUsers = async (ids) => {
    try {
      const response = await fetch(`${API_BASE_URL}/delete`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}` 
        },
        body: JSON.stringify(ids) 
      });
      if (await handleApiResponse(response.clone(), setError, setSuccessMsg)) return;
      const result = await response.json();
      await checkSelfAction(ids, 'delete');
      setSelectedIds([]);
    } catch (err) {
      setError('Connection lost. Failed to delete users. Please try again.');
    }
  }

  const updateUserStatus = async (ids, actionPath) => {
  try {
    const response = await fetch(`${API_BASE_URL}/${actionPath}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}` 
      },
      body: JSON.stringify(ids) 
    });

    if (await handleApiResponse(response.clone(), setError, setSuccessMsg)) return;
    await checkSelfAction(ids, actionPath);
    setSelectedIds([]);

  } catch (err) {
    setError('Connection lost. Failed to update user status. Please try again.');
  }
};

  const fetchUsers = async () => {
    setIsLoading(true);
    setError('');
    
    try {
      const response = await fetch(`${API_BASE_URL}/all`, {
        headers: {
          'Authorization': `Bearer ${token}` 
        }
      });
      if (await handleApiResponse(response.clone(), setError, null)) return;

      const result = await response.json();
      
      const sortedUsers = result.data.sort((a, b) => {
        const dateA = a.lastActivityTime ? new Date(a.lastActivityTime).getTime() : 0;
        const dateB = b.lastActivityTime ? new Date(b.lastActivityTime).getTime() : 0;
        return dateB - dateA;
      });

      setUsers(sortedUsers);
    } catch (err) {
      setError('Network error.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleApiResponse = async (response, setError, setSuccessMsg) => {
    if (response.status === 401) {
      onLogout(); 
      return 'Your session has expired or your account was deleted. Redirecting...';
    }
    if (!response.ok) {
      try {
        const result = await response.json();
        return result.message || `Operation failed (Status ${response.status}).`;
      } catch {
        return `Server error: ${response.status} ${response.statusText}`;
      }
    }
    try {
      const result = await response.json();
      if (result && result.success === false) {
        return result.message || 'Operation failed.';
      }
      if (result && result.message && setSuccessMsg) {
      setSuccessMsg(result.message);
    }
    } catch {
    }
    return null;
  };


  const handleSelectAll = (e) => {
    if (e.target.checked) {
      setSelectedIds(users.map(u => u.id));
    } else {
      setSelectedIds([]);
    }
  };

  const handleSelectOne = (e, id) => {
    if (e.target.checked) {
      setSelectedIds(prev => [...prev, id]);
    } else {
      setSelectedIds(prev => prev.filter(selectedId => selectedId !== id));
    }
  };

  const formatLastSeen = (dateString) => {
    if (!dateString) return 'Never';
    
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now - date) / 1000);

    if (diffInSeconds < 60) return 'less than a minute ago';
    const diffInMinutes = Math.floor(diffInSeconds / 60);
    if (diffInMinutes < 60) return `${diffInMinutes} minute${diffInMinutes > 1 ? 's' : ''} ago`;
    const diffInHours = Math.floor(diffInMinutes / 60);
    if (diffInHours < 24) return `${diffInHours} hour${diffInHours > 1 ? 's' : ''} ago`;
    const diffInDays = Math.floor(diffInHours / 24);
    return `${diffInDays} day${diffInDays > 1 ? 's' : ''} ago`;
  };

  const handleAction = (action) => {
    console.log(`Action: ${action}, Selected Users:`, selectedIds);
    setError(''); 
    if (action === 'block') {
      updateUserStatus(selectedIds, 'block');
    }
    else if (action === 'unblock') {
      updateUserStatus(selectedIds, 'unblock');
    }
    else if (action === 'delete') {
      deleteUsers(selectedIds);
    }
    else if (action === 'delete-unverified') {
      deleteUnverifiedUsers();
    }
  };

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">User Management</h2>
        <div className="d-flex gap-2">
          <Button 
            variant="outline-primary" 
            onClick={handleSendVerification}
            disabled={isVerifying}
          >
            {isVerifying && !showVerifyModal ? 'Sending...' : 'Verify account'}
          </Button>
          <Button variant="outline-secondary" onClick={onLogout}>Logout</Button>
        </div>
      </div>

      {error && <Alert variant="danger" className="rounded-1">{error}</Alert>}
      {successMsg && <Alert variant="success" className="rounded-1">{successMsg}</Alert>}
      <div className="d-flex gap-2 mb-3">
        <OverlayTrigger
          placement="top"
          overlay={<Tooltip id="tooltip-block">Block selected users</Tooltip>}
        >
          <span>
            <Button 
              variant="outline-primary" 
              className="d-flex align-items-center gap-2"
              onClick={() => handleAction('block')}
              disabled={selectedIds.length === 0}
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
              </svg>
              Block
            </Button>
          </span>
        </OverlayTrigger>

        <OverlayTrigger
          placement="top"
          overlay={<Tooltip id="tooltip-unblock">Unblock selected users</Tooltip>}
        >
          <span className="d-inline-block">
            <Button 
              variant="outline-secondary" 
              onClick={() => handleAction('unblock')}
              disabled={selectedIds.length === 0}
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
                <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
              </svg>
            </Button>
          </span>
        </OverlayTrigger>

        <OverlayTrigger
          placement="top"
          overlay={<Tooltip id="tooltip-delete">Delete selected users</Tooltip>}
        >
          <span className="d-inline-block">
            <Button 
              variant="outline-danger" 
              onClick={() => handleAction('delete')}
              disabled={selectedIds.length === 0}
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="3 6 5 6 21 6"></polyline>
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
              </svg>
            </Button>
          </span>
        </OverlayTrigger>

        <OverlayTrigger
          placement="top"
          overlay={<Tooltip id="tooltip-delete-unverified">Delete all unverified users</Tooltip>}
        >
          <span className="d-inline-block">
            <Button 
              variant="outline-danger" 
              onClick={() => handleAction('delete-unverified')}
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
                <circle cx="8.5" cy="7" r="4"></circle>
                <line x1="18" y1="8" x2="23" y2="13"></line>
                <line x1="23" y1="8" x2="18" y2="13"></line>
              </svg>
            </Button>
          </span>
        </OverlayTrigger>
      </div>

      <div className="table-responsive border rounded bg-white shadow-sm">
        <Table hover className="mb-0 align-middle">
          <thead className="table-light">
            <tr>
              <th className="text-center" style={{ width: '50px' }}>
                <Form.Check 
                  type="checkbox" 
                  onChange={handleSelectAll}
                  checked={users.length > 0 && selectedIds.length === users.length}
                />
              </th>
              <th>Name</th>
              <th>Email</th>
              <th>Status</th>
              <th>Last seen</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan="5" className="text-center py-4 text-muted">Loading data...</td>
              </tr>
            ) : users.length === 0 ? (
              <tr>
                <td colSpan="5" className="text-center py-4 text-muted">No users found.</td>
              </tr>
            ) : (
              users.map(user => {
                const isSelected = selectedIds.includes(user.id);
                const isBlocked = user.isBlocked || user.status === 'Blocked';

                return (
                  <tr 
                    key={user.id} 
                    className={isBlocked ? 'text-muted opacity-50' : ''}
                    style={{ cursor: 'pointer' }} 
                    onClick={(e) => {
                      if (e.target.type === 'checkbox') return;
                      const isSelected = selectedIds.includes(user.id);
                      handleSelectOne({ target: { checked: !isSelected } }, user.id);
                    }}
                  >
                    <td className="text-center">
                      <Form.Check 
                        type="checkbox"
                        checked={isSelected}
                        onChange={(e) => handleSelectOne(e, user.id)}
                      />
                    </td>
                    <td>{user.name}</td>
                    <td>{user.email}</td>
                    <td>{isBlocked ? 'Blocked' : user.status}</td>
                    <td title={user.lastActivityTime ? new Date(user.lastActivityTime).toLocaleString() : ''}>
                      {formatLastSeen(user.lastActivityTime)}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </Table>
      </div>
      <Modal 
        show={showVerifyModal} 
        onHide={() => setShowVerifyModal(false)} 
        centered
        backdrop="static" 
      >
        <Modal.Header closeButton>
          <Modal.Title>Account Verification</Modal.Title>
        </Modal.Header>
        <Modal.Body className="text-center py-4">
          <p className="text-muted mb-4">
            We've sent a 6-digit code to your email. Please enter it below.
          </p>
          
          <div className="d-flex justify-content-center mb-3">
            <Form.Control
              type="text"
              placeholder="000000"
              value={verificationCode}
              onChange={(e) => {
                const onlyNums = e.target.value.replace(/\D/g, '').slice(0, 6);
                setVerificationCode(onlyNums);
              }}
              style={{ width: '150px', fontSize: '1.5rem', textAlign: 'center', letterSpacing: '4px' }}
              disabled={timeLeft === 0}
              autoFocus
            />
          </div>

          {verifyError && <Alert variant="danger" className="py-2">{verifyError}</Alert>}

          <div className={`fs-5 fw-bold ${timeLeft < 60 ? 'text-danger' : 'text-primary'}`}>
            {timeLeft > 0 ? formatTime(timeLeft) : 'Code expired'}
          </div>
          {timeLeft === 0 && (
            <div className="text-muted small mt-2">
              Please close this window and request a new code.
            </div>
          )}
        </Modal.Body>
        <Modal.Footer className="justify-content-center">
          <Button 
            variant="primary" 
            onClick={handleVerifyCode}
            disabled={verificationCode.length !== 6 || timeLeft === 0 || isVerifying}
            className="w-50"
          >
            {isVerifying ? 'Verifying...' : 'Verify Code'}
          </Button>
        </Modal.Footer>
      </Modal>
    </Container>
  );
};

export default AdminPanel;