import type React from "react";
import { Link } from "react-router-dom";

interface Movie {
  id: string;
  name: string;
  slug: string;
  posterUrl: string;
  year: string;
  averageRating?: number;
  genres?: string[];
}

interface MovieListProps {
  movies: Movie[];
}

const MovieList: React.FC<MovieListProps> = ({ movies }) => {
  return (
    <div className="row row-cols-1 row-cols-md-2 row-cols-lg-4 g-4">
      {movies.map((movie) => (
        <div key={movie.id} className="col">
          <div className="card h-100 movie-card">
            <img
              src={movie.posterUrl || `/placeholder.jpg`}
              className="card-img-top"
              alt={movie.name}
              onError={(e) => {
                const target = e.target as HTMLImageElement;
                target.src = "/placeholder.jpg";
              }}
            />
            <div className="card-body">
              <h5 className="card-title">{movie.name}</h5>
              <div className="d-flex justify-content-between align-items-center">
                <span className="badge bg-primary">{movie.year}</span>
                <span className="badge bg-warning text-dark">
                  <i className="bi bi-star-fill me-1"></i>
                  {typeof movie.averageRating === "number"
                    ? movie.averageRating.toFixed(1)
                    : "?"}
                </span>
              </div>
              <div className="mt-2">
                {movie.genres?.slice(0, 3).map((genre, index) => (
                  <span key={index} className="badge bg-secondary me-1">
                    {genre}
                  </span>
                ))}
              </div>
            </div>
            <div className="card-footer">
              <Link to={`/movie/${movie.slug}`} className="btn btn-primary w-100">
                View Details
              </Link>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};

export default MovieList;
