import React from "react"
import ReactDOM from "react-dom/client"
import { BrowserRouter as Router, Routes, Route } from "react-router-dom"
import "./index.css"
import App from "./App"
import MovieDetail from "./MovieDetail"
import Navbar from "./components/Navbar"

const root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement)

root.render(
  <React.StrictMode>
    <Router>
      <Navbar />
      <Routes>
        <Route path="/" element={<App />} />
        <Route path="/movie/:slug" element={<MovieDetail />} />
      </Routes>
    </Router>
  </React.StrictMode>,
)
