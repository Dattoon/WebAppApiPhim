// Hàm lấy danh sách phim
async function getMovies(page = 1, limit = 12, category = '', search = '') {
    try {
        // Xây dựng URL API
        let apiUrl = `/api/movies?page=${page}&limit=${limit}`;
        if (category) {
            apiUrl += `&category=${encodeURIComponent(category)}`;
        }
        if (search) {
            apiUrl += `&search=${encodeURIComponent(search)}`;
        }

        // Gọi API
        const response = await fetch(apiUrl);

        // Kiểm tra response
        if (!response.ok) {
            throw new Error(`Server responded with status: ${response.status}`);
        }

        // Đọc dữ liệu từ response
        const data = await response.json();

        return data;
    } catch (error) {
        console.error('Error fetching movies:', error);
        throw error;
    }
}

// Hàm lấy chi tiết phim
async function getMovieDetails(id) {
    try {
        // Gọi API
        const response = await fetch(`/api/movies/${id}`);

        // Kiểm tra response
        if (!response.ok) {
            throw new Error(`Server responded with status: ${response.status}`);
        }

        // Đọc dữ liệu từ response
        const data = await response.json();

        return data;
    } catch (error) {
        console.error('Error fetching movie details:', error);
        throw error;
    }
}

// Hàm hiển thị danh sách phim
function renderMovies(movies, containerId = 'movies-container') {
    const container = document.getElementById(containerId);
    if (!container) return;

    // Xóa nội dung cũ
    container.innerHTML = '';

    // Kiểm tra nếu không có phim
    if (!movies || movies.length === 0) {
        container.innerHTML = '<p class="text-center">Không tìm thấy phim nào.</p>';
        return;
    }

    // Tạo HTML cho mỗi phim
    movies.forEach(movie => {
        const movieElement = document.createElement('div');
        movieElement.className = 'movie-card';
        movieElement.innerHTML = `
            <a href="/movie-details.html?id=${movie.id}">
                <div class="movie-poster">
                    <img src="${movie.poster || '/images/placeholder.png'}" alt="${movie.title}" onerror="this.src='/images/placeholder.png'">
                </div>
                <div class="movie-info">
                    <h3>${movie.title}</h3>
                    <p class="movie-year">${movie.year}</p>
                    <p class="movie-description">${movie.description}</p>
                </div>
            </a>
        `;
        container.appendChild(movieElement);
    });
}

// Hàm tìm kiếm phim
async function searchMovies(query, page = 1, limit = 12) {
    if (!query) return;

    try {
        const data = await getMovies(page, limit, '', query);
        renderMovies(data.results, 'search-results');

        // Hiển thị thông tin tìm kiếm
        const searchInfo = document.getElementById('search-info');
        if (searchInfo) {
            searchInfo.textContent = `Tìm thấy ${data.totalResults} kết quả cho "${query}"`;
        }

        return data;
    } catch (error) {
        console.error('Error searching movies:', error);

        // Hiển thị thông báo lỗi
        const searchResults = document.getElementById('search-results');
        if (searchResults) {
            searchResults.innerHTML = '<p class="text-center text-danger">Có lỗi xảy ra khi tìm kiếm. Vui lòng thử lại sau.</p>';
        }
    }
}

// Khởi tạo trang chủ
async function initHomePage() {
    try {
        const data = await getMovies();
        renderMovies(data.results);

        // Thiết lập nút phân trang
        setupPagination(data.page, data.totalPages);
    } catch (error) {
        console.error('Error initializing home page:', error);

        // Hiển thị thông báo lỗi
        const moviesContainer = document.getElementById('movies-container');
        if (moviesContainer) {
            moviesContainer.innerHTML = '<p class="text-center text-danger">Có lỗi xảy ra khi tải danh sách phim. Vui lòng thử lại sau.</p>';
        }
    }
}

// Khởi tạo trang chi tiết phim
async function initMovieDetailsPage() {
    // Lấy ID phim từ URL
    const urlParams = new URLSearchParams(window.location.search);
    const movieId = urlParams.get('id');

    if (!movieId) {
        window.location.href = '/';
        return;
    }

    try {
        const movie = await getMovieDetails(movieId);

        // Hiển thị thông tin phim
        document.title = `${movie.title} - Phim Hay`;

        // Hiển thị backdrop
        const backdropElement = document.getElementById('movie-backdrop');
        if (backdropElement) {
            backdropElement.style.backgroundImage = `url(${movie.backdrop || '/images/placeholder-backdrop.png'})`;
        }

        // Hiển thị poster
        const posterElement = document.getElementById('movie-poster');
        if (posterElement) {
            posterElement.src = movie.poster || '/images/placeholder.png';
            posterElement.alt = movie.title;
        }

        // Hiển thị tiêu đề
        const titleElement = document.getElementById('movie-title');
        if (titleElement) {
            titleElement.textContent = movie.title;
        }

        // Hiển thị thông tin khác
        const yearElement = document.getElementById('movie-year');
        if (yearElement) {
            yearElement.textContent = movie.year;
        }

        const durationElement = document.getElementById('movie-duration');
        if (durationElement) {
            durationElement.textContent = movie.duration;
        }

        const ratingElement = document.getElementById('movie-rating');
        if (ratingElement) {
            ratingElement.textContent = movie.rating ? `${movie.rating.toFixed(1)}/10` : 'N/A';
        }

        const descriptionElement = document.getElementById('movie-description');
        if (descriptionElement) {
            descriptionElement.textContent = movie.description;
        }

        // Hiển thị thể loại
        const genresElement = document.getElementById('movie-genres');
        if (genresElement && movie.genres) {
            genresElement.innerHTML = movie.genres.map(genre => `<span class="genre-tag">${genre}</span>`).join('');
        }

        // Hiển thị đạo diễn
        const directorElement = document.getElementById('movie-director');
        if (directorElement) {
            directorElement.textContent = movie.director || 'N/A';
        }

        // Hiển thị diễn viên
        const actorsElement = document.getElementById('movie-actors');
        if (actorsElement && movie.actors) {
            actorsElement.textContent = movie.actors.join(', ');
        }

        // Hiển thị nút xem phim nếu có
        const watchButtonElement = document.getElementById('watch-button');
        if (watchButtonElement) {
            if (movie.streamingUrl) {
                watchButtonElement.href = movie.streamingUrl;
                watchButtonElement.style.display = 'inline-block';
            } else {
                watchButtonElement.style.display = 'none';
            }
        }
    } catch (error) {
        console.error('Error initializing movie details page:', error);

        // Hiển thị thông báo lỗi
        const movieDetailsContainer = document.getElementById('movie-details-container');
        if (movieDetailsContainer) {
            movieDetailsContainer.innerHTML = '<p class="text-center text-danger">Có lỗi xảy ra khi tải thông tin phim. Vui lòng thử lại sau.</p>';
        }
    }
}

// Thiết lập phân trang
function setupPagination(currentPage, totalPages) {
    const paginationElement = document.getElementById('pagination');
    if (!paginationElement) return;

    paginationElement.innerHTML = '';

    // Nút trang trước
    const prevButton = document.createElement('button');
    prevButton.textContent = 'Trang trước';
    prevButton.disabled = currentPage <= 1;
    prevButton.addEventListener('click', () => {
        if (currentPage > 1) {
            loadPage(currentPage - 1);
        }
    });
    paginationElement.appendChild(prevButton);

    // Hiển thị thông tin trang
    const pageInfo = document.createElement('span');
    pageInfo.textContent = `Trang ${currentPage} / ${totalPages}`;
    paginationElement.appendChild(pageInfo);

    // Nút trang sau
    const nextButton = document.createElement('button');
    nextButton.textContent = 'Trang sau';
    nextButton.disabled = currentPage >= totalPages;
    nextButton.addEventListener('click', () => {
        if (currentPage < totalPages) {
            loadPage(currentPage + 1);
        }
    });
    paginationElement.appendChild(nextButton);
}

// Tải trang mới
async function loadPage(page) {
    try {
        const data = await getMovies(page);
        renderMovies(data.results);
        setupPagination(data.page, data.totalPages);

        // Cuộn lên đầu trang
        window.scrollTo(0, 0);
    } catch (error) {
        console.error('Error loading page:', error);
    }
}

// Khởi tạo trang tìm kiếm
function initSearchPage() {
    // Lấy query từ URL
    const urlParams = new URLSearchParams(window.location.search);
    const query = urlParams.get('q');

    if (!query) {
        window.location.href = '/';
        return;
    }

    // Hiển thị query trong ô tìm kiếm
    const searchInput = document.getElementById('search-input');
    if (searchInput) {
        searchInput.value = query;
    }

    // Tìm kiếm phim
    searchMovies(query);
}