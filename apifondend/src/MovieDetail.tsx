"use client"

import type React from "react"
import { useEffect, useState } from "react"
import { useParams, Link } from "react-router-dom"
import axios from "axios"

interface Movie {
  id: number
  title: string
  slug: string
  description: string
  posterUrl: string
  backdropUrl: string
  releaseYear: number
  rating: number
  duration: number
  genres: string[]
  countries: string[]
  directors: string[]
  actors: string[]
}

interface Episode {
  id: number
  title: string
  episodeNumber: number
  seasonNumber: number
  videoUrl: string
  thumbnailUrl: string
  duration: number
}

const MovieDetail: React.FC = () => {
  const { slug } = useParams<{ slug: string }>()
  const [movie, setMovie] = useState<Movie | null>(null)
  const [episodes, setEpisodes] = useState<Episode[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const fetchMovieDetails = async () => {
      try {
        setLoading(true)
        const movieResponse = await axios.get(`https://localhost:7056/api/movies/${slug}`)
        setMovie(movieResponse.data)

        // Fetch episodes if available
        try {
          const episodesResponse = await axios.get(`https://localhost:7056/api/streaming/${slug}/episodes`)
          setEpisodes(episodesResponse.data || [])
        } catch (episodeErr) {
          console.log("No episodes available or series-only content")
        }
      } catch (err) {
        const errorMessage = axios.isAxiosError(err)
          ? `Error: ${err.response?.status} - ${err.response?.statusText}`
          : "Failed to fetch movie details"
        setError(errorMessage)
        console.error("Fetch error:", err)
      } finally {
        setLoading(false)
      }
    }

    if (slug) {
      fetchMovieDetails()
    }
  }, [slug])

  if (loading) {
    return <div className="text-center my-5">Loading...</div>
  }

  if (error) {
    return <div className="alert alert-danger text-center">{error}</div>
  }

  if (!movie) {
    return <div className="alert alert-warning text-center">Movie not found</div>
  }

  return (
    <div className="movie-detail">
      {/* Backdrop with overlay */}
      <div
        className="backdrop"
        style={{
          backgroundImage: `url(${movie.backdropUrl || movie.posterUrl})`,
          height: "500px",
          backgroundSize: "cover",
          backgroundPosition: "center",
          position: "relative",
        }}
      >
        <div
          style={{
            position: "absolute",
            top: 0,
            left: 0,
            width: "100%",
            height: "100%",
            background: "linear-gradient(to bottom, rgba(0,0,0,0.7) 0%, rgba(0,0,0,0.9) 100%)",
          }}
        >
          <div className="container h-100">
            <div className="row h-100 align-items-center">
              <div className="col-md-4">
                <img
                  src={movie.posterUrl || "/placeholder.jpg"}
                  alt={movie.title}
                  className="img-fluid rounded shadow"
                  style={{ maxHeight: "400px" }}
                />
              </div>
              <div className="col-md-8 text-white">
                <h1>
                  {movie.title} <span className="text-muted">({movie.releaseYear})</span>
                </h1>
                <div className="mb-3">
                  {movie.genres?.map((genre, index) => (
                    <span key={index} className="badge bg-primary me-2">
                      {genre}
                    </span>
                  ))}
                  <span className="badge bg-warning text-dark ms-2">
                    <i className="bi bi-star-fill me-1"></i> {movie.rating.toFixed(1)}
                  </span>
                  <span className="ms-3">
                    {Math.floor(movie.duration / 60)}h {movie.duration % 60}m
                  </span>
                </div>
                <p className="lead">{movie.description}</p>

                {movie.directors?.length > 0 && (
                  <p>
                    <strong>Director:</strong> {movie.directors.join(", ")}
                  </p>
                )}

                {movie.actors?.length > 0 && (
                  <p>
                    <strong>Cast:</strong> {movie.actors.join(", ")}
                  </p>
                )}

                {movie.countries?.length > 0 && (
                  <p>
                    <strong>Country:</strong> {movie.countries.join(", ")}
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Episodes section */}
      {episodes.length > 0 && (
        <div className="container mt-5">
          <h2 className="mb-4">Episodes</h2>
          <div className="row row-cols-1 row-cols-md-2 row-cols-lg-4 g-4">
            {episodes.map((episode) => (
              <div key={episode.id} className="col">
                <div className="card h-100">
                  <img
                    src={episode.thumbnailUrl || "/episode-placeholder.jpg"}
                    className="card-img-top"
                    alt={episode.title}
                    style={{ height: "150px", objectFit: "cover" }}
                  />
                  <div className="card-body">
                    <h5 className="card-title">
                      {episode.seasonNumber > 0 && `S${episode.seasonNumber}:E${episode.episodeNumber} - `}
                      {episode.title}
                    </h5>
                    <p className="card-text text-muted">
                      {Math.floor(episode.duration / 60)}m {episode.duration % 60}s
                    </p>
                  </div>
                  <div className="card-footer">
                    <a
                      href={episode.videoUrl}
                      className="btn btn-primary w-100"
                      target="_blank"
                      rel="noopener noreferrer"
                    >
                      Watch
                    </a>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="container my-5">
        <Link to="/" className="btn btn-secondary">
          <i className="bi bi-arrow-left me-2"></i> Back to Movies
        </Link>
      </div>
    </div>
  )
}

export default MovieDetail
