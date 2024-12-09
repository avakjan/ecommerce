import React, { useEffect, useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { useParams } from 'react-router-dom';
import { AuthContext } from '../../context/AuthContext';

const OrderDetails = () => {
  const { id } = useParams();
  const { isAuthenticated } = useContext(AuthContext);
  const [order, setOrder] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isAuthenticated) return;

    const fetchOrder = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await axios.get(`/api/account/orderdetails/${id}`, {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        setOrder(response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch order details.');
      }
    };

    fetchOrder();
  }, [id, isAuthenticated]);

  if (!isAuthenticated) return <p>Please log in to view order details.</p>;

  if (error) return <p style={{ color: 'red' }}>{error}</p>;
  if (!order) return <p>Loading...</p>;

  return (
    <div>
      <h2>Order #{order.OrderId} Details</h2>
      <p>Status: {order.Status}</p>
      <p>Total Amount: €{order.TotalAmount}</p>
      
      <h3>Shipping Details:</h3>
      <p>Address: {order.ShippingDetails.address}</p>
      <p>City: {order.ShippingDetails.city}</p>
      <p>Postal Code: {order.ShippingDetails.postalCode}</p>
      <p>Country: {order.ShippingDetails.country}</p>

      <h3>Items:</h3>
      <ul>
        {order.OrderItems.map(oi => (
          <li key={oi.OrderItemId}>
            {oi.Item.Name} - Size: {oi.Size.Name} - Quantity: {oi.Quantity} - Unit Price: €{oi.UnitPrice}
          </li>
        ))}
      </ul>
    </div>
  );
};

export default OrderDetails;