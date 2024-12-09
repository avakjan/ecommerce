import React, { useEffect, useState } from 'react';
import axios from '../../axiosConfig';
import { useLocation, Link } from 'react-router-dom';

const SearchResults = () => {
  const [results, setResults] = useState([]);
  const [error, setError] = useState('');
  
  const query = new URLSearchParams(useLocation().search).get('query') || '';

  useEffect(() => {
    const fetchSearchResults = async () => {
      if (!query) return;
      try {
        const response = await axios.get(`/api/items/search?query=${query}`);
        setResults(response.data);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch search results.');
      }
    };

    fetchSearchResults();
  }, [query]);

  return (
    <div>
      <h2>Search Results for "{query}"</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {results.length === 0 ? (
        <p>No products found.</p>
      ) : (
        <ul>
          {results.map(item => (
            <li key={item.ItemId}>
              <Link to={`/products/${item.ItemId}`}>{item.Name}</Link> - €{item.Price}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
};

export default SearchResults;