import React from 'react';
import { Link } from 'react-router-dom';

const AdminDashboard = () => {
  return (
    <div>
      <h2>Admin Dashboard</h2>
      <ul>
        <li><Link to="/admin/products">Manage Products</Link></li>
        <li><Link to="/admin/categories">Manage Categories</Link></li>
        <li><Link to="/admin/sizes">Manage Sizes</Link></li>
        <li><Link to="/admin/orders">Manage Orders</Link></li>
      </ul>
    </div>
  );
};

export default AdminDashboard;