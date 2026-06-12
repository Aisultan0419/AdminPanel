import React, { useState, useEffect } from 'react';
import { Container, Table, Button, Form, Alert } from 'react-bootstrap';

const AdminPanel = ({ token, onLogout }) => {
  const [users, setUsers] = useState([]);
  const [selectedIds, setSelectedIds] = useState([]);
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    setIsLoading(true);
    setError('');
    
    try {
      const response = await fetch('https://localhost:7082/api/User/all', {
        headers: {
          'Authorization': `Bearer ${token}` 
        }
      });
      
      const result = await response.json();
      
      if (!response.ok || !result.success) {
        setError(result.message || 'Failed to load user data. Please refresh the page.');
        return;
      }

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
  };

  return (
    <Container className="py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h2 className="mb-0">User Management</h2>
        <Button variant="outline-secondary" onClick={onLogout}>Logout</Button>
      </div>

      {error && <Alert variant="danger" className="rounded-1">{error}</Alert>}

      <div className="d-flex gap-2 mb-3">
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

        <Button 
          variant="outline-secondary" 
          onClick={() => handleAction('unblock')}
          disabled={selectedIds.length === 0}
          title="Unblock"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
            <path d="M7 11V7a5 5 0 0 1 9.9-1"></path>
          </svg>
        </Button>

        <Button 
          variant="outline-danger" 
          onClick={() => handleAction('delete')}
          disabled={selectedIds.length === 0}
          title="Delete"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="3 6 5 6 21 6"></polyline>
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
          </svg>
        </Button>

        <Button 
          variant="outline-danger" 
          onClick={() => handleAction('delete-unverified')}
          title="Delete Unverified"
        >

          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <path d="M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"></path>
            <circle cx="8.5" cy="7" r="4"></circle>
            <line x1="18" y1="8" x2="23" y2="13"></line>
            <line x1="23" y1="8" x2="18" y2="13"></line>
          </svg>
        </Button>
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
    </Container>
  );
};

export default AdminPanel;