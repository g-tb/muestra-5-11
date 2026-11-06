# OMDb fixtures

Real responses from omdbapi.com, saved once on 2026-11-06. Tests read them through a fake
`HttpMessageHandler`; they never call the real API. None of them contains an API key.

| File | Request | HTTP |
|---|---|---|
| `search-matrix.json` | `s=matrix&type=movie` (10 results) | 200 |
| `search-no-poster.json` | `s=matrix&type=movie&page=3` (includes `Poster: N/A`) | 200 |
| `search-not-found.json` | `s=zzqxjvnotamovie&type=movie` | 200 |
| `search-too-many.json` | `s=a&type=movie` | 200 |
| `movie-tt0133093.json` | `i=tt0133093` (The Matrix) | 200 |
| `movie-rating-na.json` | `i=tt21361444` (`imdbRating: N/A`) | 200 |
| `movie-no-actors.json` | `i=tt1842414` (`Actors: N/A`, `Poster: N/A`) | 200 |
| `movie-not-found.json` | `i=tt9999999999` | 200 |
| `error-invalid-key.json` | any request with a wrong key | 401 |
| `error-limit-reached.json` | **hand-written** from OMDb's documented body; the limit can't be triggered safely | 401 |
