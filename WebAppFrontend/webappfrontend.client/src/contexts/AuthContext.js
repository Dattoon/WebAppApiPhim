"use client"

import { createContext, useState, useEffect } from "react"
import { API_URL } from "./config"

export const AuthContext = createContext()

export const AuthProvider = ({ children }) => {
    const [currentUser, setCurrentUser] = useState(null)
    const [loading, setLoading] = useState(true)
    const [error, setError] = useState("")

    useEffect(() => {
        // Check if user is logged in on page load
        const token = localStorage.getItem("token")
        const user = localStorage.getItem("user")

        if (token && user) {
            setCurrentUser(JSON.parse(user))
        }

        setLoading(false)
    }, [])

    const login = async (emailOrUsername, password) => {
        try {
            const response = await fetch(`${API_URL}/api/auth/login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ emailOrUsername, password }),
            })

            const data = await response.json()

            if (!response.ok) {
                throw new Error(data.message || "Login failed")
            }

            localStorage.setItem("token", data.token)
            localStorage.setItem("user", JSON.stringify(data.user))
            setCurrentUser(data.user)
            setError("")
            return data
        } catch (error) {
            setError(error.message)
            throw error
        }
    }

    const register = async (username, email, displayName, password) => {
        try {
            const response = await fetch(`${API_URL}/api/auth/register`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ username, email, displayName, password, confirmPassword: password }),
            })

            const data = await response.json()

            if (!response.ok) {
                throw new Error(data.message || "Registration failed")
            }

            localStorage.setItem("token", data.token)
            localStorage.setItem("user", JSON.stringify(data.user))
            setCurrentUser(data.user)
            setError("")
            return data
        } catch (error) {
            setError(error.message)
            throw error
        }
    }

    const logout = () => {
        localStorage.removeItem("token")
        localStorage.removeItem("user")
        setCurrentUser(null)
    }

    const updateProfile = async (displayName, avatarUrl) => {
        try {
            const token = localStorage.getItem("token")
            const response = await fetch(`${API_URL}/api/user/profile`, {
                method: "PUT",
                headers: {
                    "Content-Type": "application/json",
                    Authorization: `Bearer ${token}`,
                },
                body: JSON.stringify({ displayName, avatarUrl }),
            })

            const data = await response.json()

            if (!response.ok) {
                throw new Error(data.message || "Failed to update profile")
            }

            const updatedUser = { ...currentUser, ...data }
            localStorage.setItem("user", JSON.stringify(updatedUser))
            setCurrentUser(updatedUser)
            return data
        } catch (error) {
            setError(error.message)
            throw error
        }
    }

    const value = {
        currentUser,
        loading,
        error,
        login,
        register,
        logout,
        updateProfile,
    }

    return <AuthContext.Provider value={value}>{!loading && children}</AuthContext.Provider>
}
