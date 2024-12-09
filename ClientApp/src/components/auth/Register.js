import React, { useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { useNavigate } from 'react-router-dom';

const Register = () => {
  const { login } = useContext(AuthContext);
  const navigate = useNavigate();

  const [formData, setFormData] = useState({
    email: '',
    password: '',
    confirmPassword: ''
  });

  const [errors, setErrors] = useState({});
  const [serverError, setServerError] = useState('');

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
    setErrors({});
    setServerError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Basic client-side validation
    if (formData.password !== formData.confirmPassword) {
      setErrors({ confirmPassword: 'Passwords do not match.' });
      return;
    }

    try {
      const response = await axios.post('/api/account/register', {
        email: formData.email,
        password: formData.password
      });

      // Assume backend returns a token upon successful registration
      const token = response.data.token; // Adjust based on your API
      const userData = { email: formData.email, role: 'User' }; // Adjust based on response

      login(token, userData);
      navigate('/user/dashboard');
    } catch (error) {
      if (error.response && error.response.data) {
        setServerError(error.response.data.message || 'Registration failed.');
        setErrors(error.response.data.errors || {});
      } else {
        setServerError('An unexpected error occurred.');
      }
    }
  };

  return (
    <div>
      <h2>Register</h2>
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
          <label>Confirm Password:</label>
          <input 
            type="password" 
            name="confirmPassword" 
            value={formData.confirmPassword} 
            onChange={handleChange} 
            required 
          />
          {errors.confirmPassword && <span style={{ color: 'red' }}>{errors.confirmPassword}</span>}
        </div>

        <button type="submit">Register</button>
      </form>
    </div>
  );
};

export default Register;