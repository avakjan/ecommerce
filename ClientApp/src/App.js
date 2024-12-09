// src/App.js
import React from 'react';
import { Route, Routes } from 'react-router-dom'; // Removed BrowserRouter as Router

// Import components
import Register from './components/auth/Register';
import Login from './components/auth/Login';
import UserDashboard from './components/User/UserDashboard';
import AdminDashboard from './components/admin/AdminDashboard';
import ProductList from './components/pages/ProductList';
import ProductDetails from './components/pages/ProductDetails';
import Cart from './components/pages/Cart';
import Checkout from './components/pages/Checkout';
import SearchResults from './components/pages/SearchResults';
import OrderDetails from './components/User/OrderDetails';
import ProtectedRoute from './components/auth/ProtectedRoute';
import Navbar from './components/layout/Navbar';
import Home from './components/pages/Home'; // Assuming you have a Home component

function App() {
  return (
    <>
      <Navbar />
      <Routes>
        {/* Public Routes */}
        <Route path="/" element={<Home />} />
        <Route path="/register" element={<Register />} />
        <Route path="/login" element={<Login />} />
        <Route path="/products/:id" element={<ProductDetails />} />
        <Route path="/search" element={<SearchResults />} />

        {/* Protected User Routes */}
        <Route 
          path="/user/dashboard" 
          element={
            <ProtectedRoute>
              <UserDashboard />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/cart" 
          element={
            <ProtectedRoute>
              <Cart />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/checkout" 
          element={
            <ProtectedRoute>
              <Checkout />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/orders/:id" 
          element={
            <ProtectedRoute>
              <OrderDetails />
            </ProtectedRoute>
          } 
        />

        {/* Protected Admin Routes */}
        <Route 
          path="/admin/dashboard" 
          element={
            <ProtectedRoute roles={['Admin']}>
              <AdminDashboard />
            </ProtectedRoute>
          } 
        />
        {/* Add more admin routes as needed */}

      </Routes>
    </>
  );
}

export default App;