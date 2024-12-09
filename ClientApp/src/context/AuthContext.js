// src/context/AuthContext.js
import React, { createContext, useState, useEffect } from 'react';
import axios from '../axiosConfig'; // Corrected path
import { useNavigate } from 'react-router-dom';

// Create Context
export const AuthContext = createContext();

// Provider Component
export const AuthProvider = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState(null); // { email: '', role: '' }
  const navigate = useNavigate(); // Hook used inside component

  useEffect(() => {
    // Check if user is authenticated by verifying token
    const verifyToken = async () => {
      const token = localStorage.getItem('token');
      if (token) {
        try {
          // Optionally, verify token with a backend endpoint
          // For demonstration, we'll assume the token is valid and decode it
          // You can use libraries like jwt-decode to extract user info from token
          // Here, we'll fetch user details from a protected endpoint
          const response = await axios.get('/api/account/myorders'); // Adjust endpoint as needed
          
          // Assuming response is successful, set authentication state
          setIsAuthenticated(true);
          
          // Fetch user details (you might need a separate endpoint)
          // For demonstration, we'll set a static user role
          setUser({ email: 'user@example.com', role: 'User' }); // Replace with actual data
        } catch (error) {
          console.error('Token verification failed:', error);
          localStorage.removeItem('token');
          setIsAuthenticated(false);
          setUser(null);
          navigate('/login'); // Redirect to login on failure
        }
      }
    };

    verifyToken();
  }, [navigate]);

  const login = (token, userData) => {
    localStorage.setItem('token', token);
    setUser(userData); // e.g., { email: 'user@example.com', role: 'Admin' }
    setIsAuthenticated(true);
  };

  const logout = async () => {
    try {
      await axios.post('/api/account/logout');
    } catch (error) {
      console.error('Logout failed:', error);
    }
    localStorage.removeItem('token');
    setUser(null);
    setIsAuthenticated(false);
    navigate('/');
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};