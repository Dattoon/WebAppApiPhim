import { BrowserRouter as Router, Route, Routes, Navigate } from "react-router-dom"
import { Container } from "reactstrap"
import NavMenu from "./components/NavMenu"
import Home from "./components/Home"
import Login from "./components/Login"
import Register from "./components/Register"
import MovieDetail from "./components/MovieDetail"
import MovieSearch from "./components/MovieSearch"
import Profile from "./components/Profile"
import Favorites from "./components/Favorites"
import WatchHistory from "./components/WatchHistory"
import { AuthProvider } from "./contexts/AuthContext"
import PrivateRoute from "./components/PrivateRoute"
import "./custom.css"

function App() {
    return (
        <AuthProvider>
            <Router>
                <div>
                    <NavMenu />
                    <Container>
                        <Routes>
                            <Route path="/" element={<Home />} />
                            <Route path="/login" element={<Login />} />
                            <Route path="/register" element={<Register />} />
                            <Route path="/search" element={<MovieSearch />} />
                            <Route path="/movie/:slug" element={<MovieDetail />} />
                            <Route
                                path="/profile"
                                element={
                                    <PrivateRoute>
                                        <Profile />
                                    </PrivateRoute>
                                }
                            />
                            <Route
                                path="/favorites"
                                element={
                                    <PrivateRoute>
                                        <Favorites />
                                    </PrivateRoute>
                                }
                            />
                            <Route
                                path="/history"
                                element={
                                    <PrivateRoute>
                                        <WatchHistory />
                                    </PrivateRoute>
                                }
                            />
                            <Route path="*" element={<Navigate to="/" replace />} />
                        </Routes>
                    </Container>
                </div>
            </Router>
        </AuthProvider>
    )
}

export default App
