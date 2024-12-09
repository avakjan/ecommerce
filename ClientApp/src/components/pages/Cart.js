import React, { useEffect, useState } from 'react';
import axios from '../../axiosConfig';
import { Link, useNavigate } from 'react-router-dom';

const Cart = () => {
  const navigate = useNavigate();
  const [cart, setCart] = useState([]);
  const [total, setTotal] = useState(0);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchCart = async () => {
      try {
        const response = await axios.get('/api/cart');
        setCart(response.data.items);
        setTotal(response.data.total);
      } catch (err) {
        console.error(err);
        setError('Failed to fetch cart.');
      }
    };

    fetchCart();
  }, []);

  const handleRemove = async (itemId, sizeId) => {
    try {
      await axios.delete(`/api/cart/remove?itemId=${itemId}&sizeId=${sizeId}`);
      setCart(cart.filter(item => !(item.ItemId === itemId && item.SizeId === sizeId)));
    } catch (err) {
      console.error(err);
      setError('Failed to remove item from cart.');
    }
  };

  const handleUpdate = async () => {
    try {
      await axios.put('/api/cart/update', cart);
      alert('Cart updated successfully.');
    } catch (err) {
      console.error(err);
      setError('Failed to update cart.');
    }
  };

  const proceedToCheckout = () => {
    navigate('/checkout');
  };

  return (
    <div>
      <h2>Your Cart</h2>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      {cart.length === 0 ? (
        <p>Your cart is empty.</p>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <th>Item</th>
                <th>Size</th>
                <th>Quantity</th>
                <th>Price</th>
                <th>Action</th>
              </tr>
            </thead>
            <tbody>
              {cart.map(item => (
                <tr key={`${item.ItemId}-${item.SizeId}`}>
                  <td>{item.ItemId}</td> {/* Replace with item name */}
                  <td>{item.SizeId}</td> {/* Replace with size name */}
                  <td>
                    <input 
                      type="number" 
                      min="1" 
                      value={item.Quantity} 
                      onChange={(e) => {
                        const updatedCart = cart.map(ci => {
                          if (ci.ItemId === item.ItemId && ci.SizeId === item.SizeId) {
                            return { ...ci, Quantity: Number(e.target.value) };
                          }
                          return ci;
                        });
                        setCart(updatedCart);
                      }}
                    />
                  </td>
                  <td>€{item.Quantity * item.UnitPrice}</td>
                  <td>
                    <button onClick={() => handleRemove(item.ItemId, item.SizeId)}>Remove</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <h3>Total: €{total}</h3>
          <button onClick={handleUpdate}>Update Cart</button>
          <button onClick={proceedToCheckout}>Proceed to Checkout</button>
        </>
      )}
    </div>
  );
};

export default Cart;