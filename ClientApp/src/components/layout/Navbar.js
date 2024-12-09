import React, { useContext, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthContext } from '../../context/AuthContext';
import axios from '../../axiosConfig';

const Navbar = () => {
  const { isAuthenticated, user, logout } = useContext(AuthContext);
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');

  const handleLogout = async () => {
    try {
      const token = localStorage.getItem('token');
      await axios.post('/api/account/logout', {}, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      logout();
      navigate('/');
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  const handleSearch = (e) => {
    e.preventDefault();
    if (searchQuery.trim() !== '') {
      navigate(`/search?query=${searchQuery}`);
      setSearchQuery('');
    }
  };

  return (
    <nav>
      <Link to="/">Home</Link> |{' '}
      <form onSubmit={handleSearch} style={{ display: 'inline' }}>
        <input 
          type="text" 
          value={searchQuery} 
          onChange={(e) => setSearchQuery(e.target.value)} 
          placeholder="Search..."
        />
        <button type="submit">Search</button>
      </form> |{' '}
      {isAuthenticated ? (
        <>
          {user.role === 'Admin' && <Link to="/admin/dashboard">Admin Dashboard</Link>} |{' '}
          <Link to="/user/dashboard">User Dashboard</Link> |{' '}
          <Link to="/cart">Cart</Link> |{' '}
          <button onClick={handleLogout}>Logout</button>
        </>
      ) : (
        <>
          <Link to="/login">Login</Link> |{' '}
          <Link to="/register">Register</Link>
        </>
      )}
    </nav>
  );
};

export default Navbar;