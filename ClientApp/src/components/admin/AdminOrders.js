import React, { useEffect, useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { Link } from 'react-router-dom';

const AdminOrders = () => {
  const { user } = useContext(AuthContext);
  const [orders, setOrders] = useState([]);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchOrders = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await axios.get('/api/admin/orders', {
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

  const handleUpdateStatus = async (orderId, newStatus) => {
    try {
      const token = localStorage.getItem('token');
      await axios.post(`/api/admin/orders/${orderId}/update-status`, { Status: newStatus }, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      setOrders(orders.map(order => 
        order.OrderId === orderId ? { ...order, Status: newStatus } : order
      ));
    } catch (err) {
      console.error(err);
      setError('Failed to update order status.');
    }
  };

  return (
    <div>
      <h2>Manage Orders</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {orders.length === 0 ? (
        <p>No orders available.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Order ID</th>
              <th>User ID</th>
              <th>Status</th>
              <th>Total (€)</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {orders.map(order => (
              <tr key={order.OrderId}>
                <td>{order.OrderId}</td>
                <td>{order.UserId}</td>
                <td>{order.Status}</td>
                <td>€{order.TotalAmount}</td>
                <td>
                  <Link to={`/admin/orders/${order.OrderId}`}>View Details</Link> |{' '}
                  <select 
                    value={order.Status} 
                    onChange={(e) => handleUpdateStatus(order.OrderId, e.target.value)}
                  >
                    <option value="Pending">Pending</option>
                    <option value="Processing">Processing</option>
                    <option value="Shipped">Shipped</option>
                    <option value="Completed">Completed</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default AdminOrders;