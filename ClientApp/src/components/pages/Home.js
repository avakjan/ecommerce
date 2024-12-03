import React, { useState, useEffect } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Container,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Typography,
  Button,
  Box,
  CircularProgress,
} from '@mui/material';
import { productsApi } from '../../services/api';

const Home = () => {
  const [featuredProducts, setFeaturedProducts] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchFeaturedProducts = async () => {
      try {
        const response = await productsApi.getFeatured();
        setFeaturedProducts(response.data);
      } catch (err) {
        setError('Failed to load featured products');
        console.error('Error fetching featured products:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchFeaturedProducts();
  }, []);

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
        <Typography color="error">{error}</Typography>
      </Box>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom align="center">
        Welcome to Our Online Shop
      </Typography>

      <Typography variant="h6" gutterBottom align="center" color="text.secondary" sx={{ mb: 4 }}>
        Discover our featured products
      </Typography>

      <Grid container spacing={4}>
        {featuredProducts.map((product) => (
          <Grid item key={product.id} xs={12} sm={6} md={4}>
            <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardMedia
                component="img"
                sx={{
                  height: 200,
                  objectFit: 'cover',
                }}
                image={product.imageUrl || '/placeholder.jpg'}
                alt={product.name}
              />
              <CardContent sx={{ flexGrow: 1 }}>
                <Typography gutterBottom variant="h6" component="h2">
                  {product.name}
                </Typography>
                <Typography color="text.secondary" paragraph>
                  {product.description}
                </Typography>
                <Typography variant="h6" color="primary">
                  ${product.price.toFixed(2)}
                </Typography>
                <Button
                  component={RouterLink}
                  to={`/products/${product.id}`}
                  variant="contained"
                  color="primary"
                  fullWidth
                  sx={{ mt: 2 }}
                >
                  View Details
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>

      <Box display="flex" justifyContent="center" mt={6}>
        <Button
          component={RouterLink}
          to="/products"
          variant="contained"
          color="primary"
          size="large"
        >
          View All Products
        </Button>
      </Box>
    </Container>
  );
};

export default Home;
