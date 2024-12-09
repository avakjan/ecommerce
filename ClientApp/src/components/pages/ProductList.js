import React, { useEffect, useState } from 'react';
import axios from '../../axiosConfig';
import { Link, useSearchParams } from 'react-router-dom';

const ProductList = () => {
  const [items, setItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [selectedCategory, setSelectedCategory] = useState(0);
  const [error, setError] = useState('');

  const [searchParams, setSearchParams] = useSearchParams();
  const query = searchParams.get('query') || '';

  useEffect(() => {
    const fetchCategories = async () => {
      try {
        const response = await axios.get('/api/admin/categories');
        setCategories(response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch categories.');
      }
    };

    fetchCategories();
  }, []);

  useEffect(() => {
    const fetchItems = async () => {
      try {
        let endpoint = '/api/items';
        if (selectedCategory !== 0) {
          endpoint += `?categoryId=${selectedCategory}`;
        } else if (query) {
          endpoint = `/api/items/search?query=${query}`;
        }
        const response = await axios.get(endpoint);
        setItems(response.data.Items || response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch items.');
      }
    };

    fetchItems();
  }, [selectedCategory, query]);

  const handleCategoryChange = (e) => {
    setSelectedCategory(Number(e.target.value));
  };

  const handleSearch = (e) => {
    e.preventDefault();
    const searchQuery = e.target.elements.search.value;
    setSearchParams({ query: searchQuery });
  };

  return (
    <div>
      <h2>All Products</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {/* Search Form */}
      <form onSubmit={handleSearch}>
        <input type="text" name="search" placeholder="Search products..." />
        <button type="submit">Search</button>
      </form>

      {/* Category Filter */}
      <div>
        <label>Filter by Category:</label>
        <select value={selectedCategory} onChange={handleCategoryChange}>
          <option value={0}>All Categories</option>
          {categories.map(cat => (
            <option key={cat.CategoryId} value={cat.CategoryId}>{cat.Name}</option>
          ))}
        </select>
      </div>

      {/* Product Listings */}
      <ul>
        {items.map(item => (
          <li key={item.ItemId}>
            <Link to={`/products/${item.ItemId}`}>{item.Name}</Link> - €{item.Price}
          </li>
        ))}
      </ul>
    </div>
  );
};

export default ProductList;