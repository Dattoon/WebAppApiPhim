"use client"
import { API_URL } from '../../config';  // Không phải '../config'
import { useState, useEffect } from "react"
import { Link } from "react-router-dom"


const MovieList = () => {
    const [movies, setMovies] = useState([])
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState(null)
    const [page, setPage] = useState(1)
    const [totalPages, setTotalPages] = useState(1)

    useEffect(() => {
        const fetchMovies = async () => {
            try {
                setLoading(true)
                const response = await fetch(`${API_URL}/api/movies/latest?page=${page}&limit=8`)

                if (!response.ok) {
                    throw new Error(`API error: ${response.status}`)
                }

                const data = await response.json()
                setMovies(data.data || [])
                setTotalPages(data.pagination?.total_pages || 1)
            } catch (err) {
                setError(err.message)
                console.error("Error fetching movies:", err)
            } finally {
                setLoading(false)
            }
        }

        fetchMovies()
    }, [page])

    const handlePageChange = (newPage) => {
        setPage(newPage)
        window.scrollTo(0, 0)
    }

    if (loading) {
        return <div className="text-center my-5">Loading...</div>
    }

    if (error) {
        return <div className="alert alert-danger">{error}</div>
    }

    return (
        <div>
            <h1 className="mb-4">Latest Movies</h1>

            {movies.length === 0 ? (
                <p>No movies found.</p>
            ) : (
                <>
                    <div className="row">
                        {movies.map((movie) => (
                            <div key={movie.id} className="col-md-3 mb-4">
                                <div className="card h-100">
                                    <Link to={`/movie/${movie.slug}`}>
                                        <img
                                            className="card-img-top"
                                            src={movie.posterUrl || movie.thumbUrl || "/placeholder.svg?height=300&width=200"}
                                            alt={movie.name}
                                            style={{ height: "300px", objectFit: "cover" }}
                                        />
                                    </Link>
                                    <div className="card-body">
                                        <h5 className="card-title">
                                            <Link to={`/movie/${movie.slug}`} className="text-decoration-none text-dark">
                                                {movie.name}
                                            </Link>
                                        </h5>
                                        <div className="small text-muted">
                                            {movie.year} • {movie.loai_phim}
                                        </div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <nav className="mt-4">
                            <ul className="pagination justify-content-center">
                                <li className={`page-item ${page === 1 ? "disabled" : ""}`}>
                                    <button className="page-link" onClick={() => handlePageChange(page - 1)}>
                                        Previous
                                    </button>
                                </li>

                                {[...Array(totalPages).keys()].map((i) => (
                                    <li key={i + 1} className={`page-item ${page === i + 1 ? "active" : ""}`}>
                                        <button className="page-link" onClick={() => handlePageChange(i + 1)}>
                                            {i + 1}
                                        </button>
                                    </li>
                                ))}

                                <li className={`page-item ${page === totalPages ? "disabled" : ""}`}>
                                    <button className="page-link" onClick={() => handlePageChange(page + 1)}>
                                        Next
                                    </button>
                                </li>
                            </ul>
                        </nav>
                    )}
                </>
            )}
        </div>
    )
}

export default MovieList
