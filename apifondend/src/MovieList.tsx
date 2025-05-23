"use client";
import { useCallback } from "react";
import { Link } from "react-router-dom";

interface Movie {
  id: string | number;
  slug: string;
  name: string;
  year: string | number;
  loai_phim: string;
  posterUrl?: string;
  thumbUrl?: string;
}

interface MovieListProps {
  movies: Movie[];
  page: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}

const MovieList: React.FC<MovieListProps> = ({ movies, page, totalPages, onPageChange }) => {
  // Handle page change with useCallback to prevent unnecessary re-renders
  const handlePageChange = useCallback(
    (newPage: number) => {
      if (newPage > 0 && newPage <= totalPages) {
        onPageChange(newPage);
        window.scrollTo({ top: 0, behavior: "smooth" });
      }
    },
    [onPageChange, totalPages]
  );

  // Display message if no movies are found
  if (movies.length === 0) {
    return <p className="text-center text-muted">No movies found.</p>;
  }

  // Calculate pagination range dynamically
  const maxPagesToShow = 5;
  const startPage = Math.max(1, page - Math.floor(maxPagesToShow / 2));
  const endPage = Math.min(totalPages, startPage + maxPagesToShow - 1);
  const pages = Array.from({ length: endPage - startPage + 1 }, (_, i) => startPage + i);

  return (
    <div>
      {/* Movie Grid */}
      <div className="row row-cols-1 row-cols-md-4 g-4">
        {movies.map((movie) => (
          <div key={movie.id} className="col">
            <div className="card h-100 shadow-sm">
              <Link to={`/movie/${movie.slug}`} className="text-decoration-none">
                <img
                  className="card-img-top"
                  src={movie.posterUrl || movie.thumbUrl || "/placeholder.svg?height=300&width=200"}
                  alt={movie.name}
                  style={{ height: "300px", objectFit: "cover" }}
                  onError={(e) => {
                    (e.target as HTMLImageElement).src = "/placeholder.svg?height=300&width=200";
                  }}
                  loading="lazy" // Add lazy loading for better performance
                />
              </Link>
              <div className="card-body d-flex flex-column">
                <h5 className="card-title mb-2">
                  <Link to={`/movie/${movie.slug}`} className="text-decoration-none text-dark">
                    {movie.name}
                  </Link>
                </h5>
                <div className="small text-muted mt-auto">
                  {movie.year} • {movie.loai_phim}
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <nav className="mt-4" aria-label="Movie pagination">
          <ul className="pagination justify-content-center">
            <li className={`page-item ${page === 1 ? "disabled" : ""}`}>
              <button
                className="page-link"
                onClick={() => handlePageChange(page - 1)}
                disabled={page === 1}
                aria-label="Previous page"
              >
                Previous
              </button>
            </li>

            {pages.map((p) => (
              <li key={p} className={`page-item ${page === p ? "active" : ""}`}>
                <button
                  className="page-link"
                  onClick={() => handlePageChange(p)}
                  aria-current={page === p ? "page" : undefined}
                >
                  {p}
                </button>
              </li>
            ))}

            <li className={`page-item ${page === totalPages ? "disabled" : ""}`}>
              <button
                className="page-link"
                onClick={() => handlePageChange(page + 1)}
                disabled={page === totalPages}
                aria-label="Next page"
              >
                Next
              </button>
            </li>
          </ul>
        </nav>
      )}
    </div>
  );
};

export default MovieList;