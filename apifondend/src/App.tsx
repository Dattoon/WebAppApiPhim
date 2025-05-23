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
        const response = await axios.get("https://localhost:7056/api/movies/latest", {
          params: { page, limit: 20 },
        });

        const data = response.data;
        console.log("API Response:", data);

        // ✅ Lấy danh sách phim từ đúng đường dẫn data.data.data
        setMovies(Array.isArray(data.data?.data) ? data.data.data : []);
        setTotalPages(data.data?.pagination?.total_pages || 1);
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
    <div className="container mt-4">
      <h1 className="text-center mb-4">Movie Collection</h1>

      {movies.length === 0 ? (
        <div className="alert alert-info">No movies found</div>
      ) : (
        <>
          <MovieList movies={movies} />

          <div className="d-flex justify-content-center mt-4">
            <nav aria-label="Movie pagination">
              <ul className="pagination">
                <li className={`page-item ${page === 1 ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage((p) => Math.max(1, p - 1))}>
                    Previous
                  </button>
                </li>

                {Array.from({ length: totalPages }, (_, i) => (
                  <li key={i} className={`page-item ${page === i + 1 ? "active" : ""}`}>
                    <button className="page-link" onClick={() => setPage(i + 1)}>
                      {i + 1}
                    </button>
                  </li>
                ))}

                <li className={`page-item ${page === totalPages ? "disabled" : ""}`}>
                  <button className="page-link" onClick={() => setPage((p) => Math.min(totalPages, p + 1))}>
                    Next
                  </button>
                </li>
              </ul>
            </nav>
          </div>
        </>
      )}
    </div>
  );
}

export default App;
