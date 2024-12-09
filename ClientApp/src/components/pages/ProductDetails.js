import React, { useEffect, useState } from 'react';
import axios from '../../axiosConfig';
import { useParams, useNavigate } from 'react-router-dom';

const ProductDetails = () => {
  const { id } = useParams(); // Product ID
  const navigate = useNavigate();
  
  const [item, setItem] = useState(null);
  const [sizeId, setSizeId] = useState('');
  const [quantity, setQuantity] = useState(1);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    const fetchItem = async () => {
      try {
        const response = await axios.get(`/api/items/${id}`);
        setItem(response.data.Item);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch item details.');
      }
    };

    fetchItem();
  }, [id]);

  const handleAddToCart = async () => {
    if (!sizeId) {
      setError('Please select a size.');
      return;
    }

    try {
      const response = await axios.post('/api/cart/addtocart', {
        ItemId: item.ItemId,
        SizeId: sizeId,
        Quantity: quantity
      });

      setSuccess('Item added to cart successfully!');
      setError('');
    } catch (err) {
      console.error(err);
      setError(err.response?.data?.error || 'Failed to add item to cart.');
    }
  };

  if (error) return <p style={{ color: 'red' }}>{error}</p>;
  if (!item) return <p>Loading...</p>;

  return (
    <div>
      <h2>{item.Name}</h2>
      <img src={item.ImageUrl} alt={item.Name} width="200" />
      <p>{item.Description}</p>
      <p>Price: €{item.Price}</p>
      
      {/* Size Selection */}
      <div>
        <label>Select Size:</label>
        <select value={sizeId} onChange={(e) => setSizeId(e.target.value)}>
          <option value="">--Select Size--</option>
          {item.ItemSizes.map(isz => (
            <option key={isz.SizeId} value={isz.SizeId}>
              {isz.Size.Name} (Available: {isz.Quantity})
            </option>
          ))}
        </select>
      </div>

      {/* Quantity Selection */}
      <div>
        <label>Quantity:</label>
        <input 
          type="number" 
          min="1" 
          value={quantity} 
          onChange={(e) => setQuantity(Number(e.target.value))} 
        />
      </div>

      <button onClick={handleAddToCart}>Add to Cart</button>

      {success && <p style={{ color: 'green' }}>{success}</p>}
    </div>
  );
};

export default ProductDetails;