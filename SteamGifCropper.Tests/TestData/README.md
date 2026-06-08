# Test data

Sample assets used by the test suite (copied next to the test binary via
`CopyToOutputDirectory`). The `*.gif` files and `Scroll_test_*.png` back the
GIF / scroll tests; `Sample.HEIC` backs the HEIC-decoding tests
(`WebpHeicSupportTests`). Tests that reference an asset skip gracefully if it is
missing, so a lightweight checkout still runs.

## Credits

- `Sample.HEIC` — photo by [bbfox0703](https://github.com/bbfox0703), contributed
  for use in this repository's tests.
