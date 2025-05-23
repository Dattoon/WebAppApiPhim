"use client";
import { useEffect, useState } from "react";
import axios from "axios";
import "./App.css";
import "bootstrap/dist/css/bootstrap.min.css";
import MovieList from "./MovieList";

function App() {
  const [movies, setMovies] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  useEffect(() => {
    const fetchMovies = async () => {
      try {
        setLoading(true);
        const response = await axios.get(`https://localhost:7056/api/movies/latest`, {
          params: { page, limit: 8 },
        });

        const data = response.data;
        console.log("API Response:", data);
        setMovies(data.data || []);
        setTotalPages(data.pagination?.total_pages || 1);
      } catch (err) {
        const errorMessage = axios.isAxiosError(err)
          ? `${err.message}: ${err.response?.status} - ${err.response?.statusText}`
          : "Failed to fetch movies. Check backend URL and CORS.";
        setError(errorMessage);
        console.error("Fetch error:", err);
      } finally {
        setLoading(false);
      }
    };

    fetchMovies();
  }, [page]);

  if (loading) {
    return <div className="text-center my-5">Loading...</div>;
  }

  if (error) {
    return <div className="alert alert-danger text-center">{error}</div>;
  }

  return (
    <div className="App container py-4">
      <h1 className="mb-4 text-center">Danh sách phim mới</h1>
      <MovieList movies={movies} page={page} totalPages={totalPages} onPageChange={setPage} />
    </div>
  );
}

export default App;