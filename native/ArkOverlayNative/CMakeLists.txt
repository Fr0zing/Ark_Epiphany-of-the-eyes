cmake_minimum_required(VERSION 3.20)
project(ArkOverlayNative LANGUAGES CXX)

add_library(ArkOverlayNative SHARED ArkOverlayNative.cpp)
target_compile_features(ArkOverlayNative PRIVATE cxx_std_20)
target_compile_definitions(ArkOverlayNative PRIVATE UNICODE _UNICODE WIN32_LEAN_AND_MEAN NOMINMAX)
# The source contains Russian UI literals and is stored as UTF-8 without a BOM.
# Force both the source and execution character sets so MSVC does not reinterpret
# the literals through the machine's legacy ANSI code page.
target_compile_options(ArkOverlayNative PRIVATE /utf-8 /W4 /permissive- /EHsc /O2 /GL)
target_link_options(ArkOverlayNative PRIVATE /LTCG)
target_link_libraries(ArkOverlayNative PRIVATE d3d11 dxgi d2d1 dwrite dcomp user32 dwmapi)

set_target_properties(ArkOverlayNative PROPERTIES
    OUTPUT_NAME "ArkOverlayNative"
    RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_BINARY_DIR}/out"
    LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_BINARY_DIR}/out"
)
