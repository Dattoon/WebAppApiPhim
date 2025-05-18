import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import Navbar from "./components/layout/Navbar";
import MovieList from "./components/movie/MovieList";
import MovieDetail from "./components/movie/MovieDetail";

function App() {
    return (
        <Router>
            <Navbar />
            <div className="container mt-4">
                <Routes>
                    <Route path="/" element={<MovieList />} />
                    <Route path="/movie/:slug" element={<MovieDetail />} />
                </Routes>
            </div>
        </Router>
    );
}

export default App;
