import React, { useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { useNavigate } from 'react-router-dom';

const Checkout = () => {
  const { user } = useContext(AuthContext);
  const navigate = useNavigate();

  const [shippingDetails, setShippingDetails] = useState({
    address: '',
    city: '',
    postalCode: '',
    country: ''
  });

  const [paymentMethod, setPaymentMethod] = useState('Credit Card');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleChange = (e) => {
    setShippingDetails({
      ...shippingDetails,
      [e.target.name]: e.target.value
    });
    setError('');
    setSuccess('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    // Basic validation
    if (!shippingDetails.address || !shippingDetails.city || !shippingDetails.postalCode || !shippingDetails.country) {
      setError('Please fill in all shipping details.');
      return;
    }

    try {
      const token = localStorage.getItem('token');
      const response = await axios.post('/api/cart/checkout', {
        ShippingDetails: shippingDetails,
        PaymentMethod: paymentMethod
      }, {
        headers: { 'Authorization': `Bearer ${token}` }
      });

      setSuccess(`Order placed successfully! Order ID: ${response.data.orderId}`);
      setError('');
      // Optionally, redirect to order details page
      // navigate(`/orders/${response.data.orderId}`);
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.message || 'Failed to place order.');
    }
  };

  return (
    <div>
      <h2>Checkout</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {success && <p style={{ color: 'green' }}>{success}</p>}

      <form onSubmit={handleSubmit}>
        <h3>Shipping Details</h3>
        <div>
          <label>Address:</label>
          <input 
            type="text" 
            name="address" 
            value={shippingDetails.address} 
            onChange={handleChange} 
            required 
          />
        </div>
        <div>
          <label>City:</label>
          <input 
            type="text" 
            name="city" 
            value={shippingDetails.city} 
            onChange={handleChange} 
            required 
          />
        </div>
        <div>
          <label>Postal Code:</label>
          <input 
            type="text" 
            name="postalCode" 
            value={shippingDetails.postalCode} 
            onChange={handleChange} 
            required 
          />
        </div>
        <div>
          <label>Country:</label>
          <input 
            type="text" 
            name="country" 
            value={shippingDetails.country} 
            onChange={handleChange} 
            required 
          />
        </div>

        <h3>Payment Method</h3>
        <div>
          <label>
            <input 
              type="radio" 
              name="paymentMethod" 
              value="Credit Card" 
              checked={paymentMethod === 'Credit Card'} 
              onChange={(e) => setPaymentMethod(e.target.value)} 
            />
            Credit Card
          </label>
          {/* Add more payment methods if needed */}
        </div>

        <button type="submit">Place Order</button>
      </form>
    </div>
  );
};

export default Checkout;