import React, { useEffect, useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { Link } from 'react-router-dom';

const UserDashboard = () => {
  const { user } = useContext(AuthContext);
  const [orders, setOrders] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchOrders = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await axios.get('/api/account/myorders', {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        setOrders(response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch orders.');
      }
    };

    fetchOrders();
  }, []);

  return (
    <div>
      <h2>User Dashboard</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <h3>Your Orders:</h3>
      {orders.length === 0 ? (
        <p>You have no orders.</p>
      ) : (
        <ul>
          {orders.map(order => (
            <li key={order.OrderId}>
              Order #{order.OrderId} - {order.Status} - Total: €{order.TotalAmount}
              <Link to={`/orders/${order.OrderId}`}> View Details</Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default UserDashboard;