import React, { useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { useNavigate } from 'react-router-dom';

const Login = () => {
  const { login } = useContext(AuthContext);
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    email: '',
    password: '',
    rememberMe: false
  });

  const [errors, setErrors] = useState({});
  const [serverError, setServerError] = useState('');

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;
    setFormData({
      ...formData,
      [name]: type === 'checkbox' ? checked : value
    });
    setErrors({});
    setServerError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    try {
      const response = await axios.post('/api/account/login', {
        email: formData.email,
        password: formData.password,
        rememberMe: formData.rememberMe
      });

      // Assume backend returns a token upon successful login
      const token = response.data.token; // Adjust based on your API
      const userData = { email: formData.email, role: 'User' }; // Adjust based on response

      login(token, userData);
      navigate('/user/dashboard');
    } catch (error) {
      if (error.response && error.response.status === 401) {
        setServerError('Invalid login attempt.');
      } else if (error.response && error.response.status === 423) {
        setServerError('User account is locked out.');
      } else {
        setServerError('An unexpected error occurred.');
      }
    }
  };

  return (
    <div>
      <h2>Login</h2>
      {serverError && <p style={{ color: 'red' }}>{serverError}</p>}
      <form onSubmit={handleSubmit}>
        <div>
          <label>Email:</label>
          <input 
            type="email" 
            name="email" 
            value={formData.email} 
            onChange={handleChange} 
            required 
          />
          {errors.email && <span style={{ color: 'red' }}>{errors.email}</span>}
        </div>
        
        <div>
          <label>Password:</label>
          <input 
            type="password" 
            name="password" 
            value={formData.password} 
            onChange={handleChange} 
            required 
          />
          {errors.password && <span style={{ color: 'red' }}>{errors.password}</span>}
        </div>

        <div>
          <label>
            <input 
              type="checkbox" 
              name="rememberMe" 
              checked={formData.rememberMe} 
              onChange={handleChange} 
            />
            Remember Me
          </label>
        </div>

        <button type="submit">Login</button>
      </form>
    </div>
  );
};

export default Login;