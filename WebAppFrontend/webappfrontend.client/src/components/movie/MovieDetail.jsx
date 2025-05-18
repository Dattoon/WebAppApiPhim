"use client"
import { API_URL } from '../../config';  // Không phải '../config'
import { useState, useEffect } from "react"
import { useParams, Link } from "react-router-dom"


const MovieDetail = () => {
    const { slug } = useParams()
    const [movie, setMovie] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [relatedMovies, setRelatedMovies] = useState([])

    useEffect(() => {
        const fetchMovieData = async () => {
            try {
                setLoading(true)

                // Fetch movie details
                const response = await fetch(`${API_URL}/api/movies/detail/${slug}`)

                if (!response.ok) {
                    throw new Error(`API error: ${response.status}`)
                }

                const data = await response.json()
                setMovie(data)

                // Fetch related movies
                const relatedResponse = await fetch(`${API_URL}/api/movies/related/${slug}?limit=4`)
                if (relatedResponse.ok) {
                    const relatedData = await relatedResponse.json()
                    setRelatedMovies(relatedData.data || [])
                }
            } catch (err) {
                setError(err.message)
                console.error("Error fetching movie details:", err)
            } finally {
                setLoading(false)
            }
        }

        fetchMovieData()
    }, [slug])

    if (loading) {
        return <div className="text-center my-5">Loading...</div>
    }

    if (error || !movie) {
        return <div className="alert alert-danger">{error || "Failed to load movie details"}</div>
    }

    return (
        <div className="movie-detail">
            <div className="row mb-4">
                <div className="col-md-4">
                    <img
                        src={movie.poster_url || movie.thumb_url || "/placeholder.svg?height=450&width=300"}
                        alt={movie.name}
                        className="img-fluid rounded"
                    />
                </div>
                <div className="col-md-8">
                    <h1>{movie.name}</h1>
                    {movie.original_name && <h5 className="text-muted">{movie.original_name}</h5>}

                    <div className="mb-3">
                        {movie.year && <span className="badge bg-secondary me-2">{movie.year}</span>}
                        {movie.type && <span className="badge bg-secondary me-2">{movie.type}</span>}
                        {movie.quality && <span className="badge bg-info me-2">{movie.quality}</span>}
                    </div>

                    <div className="mb-3">
                        <strong>Rating:</strong> {movie.tmdb_vote_average ? `${movie.tmdb_vote_average}/10` : "N/A"}
                    </div>

                    <div className="mb-3">
                        <strong>Genres:</strong> {movie.genres || movie.categories || "N/A"}
                    </div>

                    <div className="mb-3">
                        <strong>Country:</strong> {movie.country || movie.countries || "N/A"}
                    </div>

                    {movie.director && (
                        <div className="mb-3">
                            <strong>Director:</strong> {movie.director}
                        </div>
                    )}

                    {movie.casts && (
                        <div className="mb-3">
                            <strong>Cast:</strong> {movie.casts}
                        </div>
                    )}

                    <div className="mb-3">
                        <strong>Description:</strong>
                        <p>{movie.description || movie.content || "No description available."}</p>
                    </div>

                    <button className="btn btn-primary">Watch Now</button>
                </div>
            </div>

            {/* Related Movies */}
            {relatedMovies.length > 0 && (
                <div>
                    <h3 className="mb-3">Related Movies</h3>
                    <div className="row">
                        {relatedMovies.map((movie) => (
                            <div key={movie.id} className="col-md-3 mb-4">
                                <div className="card h-100">
                                    <Link to={`/movie/${movie.slug}`}>
                                        <img
                                            className="card-img-top"
                                            src={movie.posterUrl || movie.thumbUrl || "/placeholder.svg?height=200&width=150"}
                                            alt={movie.name}
                                            style={{ height: "200px", objectFit: "cover" }}
                                        />
                                    </Link>
                                    <div className="card-body">
                                        <h5 className="card-title">
                                            <Link to={`/movie/${movie.slug}`} className="text-decoration-none text-dark">
                                                {movie.name}
                                            </Link>
                                        </h5>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    )
}

export default MovieDetail
