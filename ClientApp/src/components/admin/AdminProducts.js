import React, { useEffect, useState, useContext } from 'react';
import axios from '../../axiosConfig';
import { AuthContext } from '../../context/AuthContext';
import { Link } from 'react-router-dom';

const AdminProducts = () => {
  const { user } = useContext(AuthContext);
  const [products, setProducts] = useState([]);
  const [error, setError] = useState('');
  const [newProduct, setNewProduct] = useState({
    Name: '',
    Price: 0,
    Description: '',
    ImageUrl: ''
    // Add other fields as necessary
  });

  useEffect(() => {
    const fetchProducts = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await axios.get('/api/admin/products', {
          headers: { 'Authorization': `Bearer ${token}` }
        });
        setProducts(response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch products.');
      }
    };

    fetchProducts();
  }, []);

  const handleChange = (e) => {
    setNewProduct({
      ...newProduct,
      [e.target.name]: e.target.value
    });
    setError('');
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    try {
      const token = localStorage.getItem('token');
      const response = await axios.post('/api/admin/products', newProduct, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      setProducts([...products, response.data]);
      setNewProduct({
        Name: '',
        Price: 0,
        Description: '',
        ImageUrl: ''
      });
    } catch (err) {
      console.error(err);
      setError('Failed to create product.');
    }
  };

  const handleDelete = async (id) => {
    try {
      const token = localStorage.getItem('token');
      await axios.delete(`/api/admin/products/${id}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      setProducts(products.filter(p => p.ItemId !== id));
    } catch (err) {
      console.error(err);
      setError('Failed to delete product.');
    }
  };

  return (
    <div>
      <h2>Manage Products</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {/* Create Product Form */}
      <h3>Create New Product</h3>
      <form onSubmit={handleCreate}>
        <div>
          <label>Name:</label>
          <input 
            type="text" 
            name="Name" 
            value={newProduct.Name} 
            onChange={handleChange} 
            required 
          />
        </div>
        <div>
          <label>Price (€):</label>
          <input 
            type="number" 
            name="Price" 
            value={newProduct.Price} 
            onChange={handleChange} 
            required 
            min="0"
            step="0.01"
          />
        </div>
        <div>
          <label>Description:</label>
          <textarea 
            name="Description" 
            value={newProduct.Description} 
            onChange={handleChange} 
            required 
          />
        </div>
        <div>
          <label>Image URL:</label>
          <input 
            type="text" 
            name="ImageUrl" 
            value={newProduct.ImageUrl} 
            onChange={handleChange} 
            required 
          />
        </div>
        <button type="submit">Create Product</button>
      </form>

      {/* Products List */}
      <h3>Existing Products</h3>
      <ul>
        {products.map(product => (
          <li key={product.ItemId}>
            {product.Name} - €{product.Price}
            <Link to={`/admin/products/edit/${product.ItemId}`}>Edit</Link> |{' '}
            <button onClick={() => handleDelete(product.ItemId)}>Delete</button>
          </li>
        ))}
      </ul>
    </div>
  );
};

export default AdminProducts;