/*
 * screencapturekit_bridge.h
 * Native ScreenCaptureKit wrapper for ShareX.Avalonia
 * 
 * This library provides a C-compatible interface for P/Invoke from .NET
 * Requires macOS 12.3+ (Monterey)
 */

#ifndef SCREENCAPTUREKIT_BRIDGE_H
#define SCREENCAPTUREKIT_BRIDGE_H

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

/**
 * Check if ScreenCaptureKit is available on this system.
 * @return 1 if available (macOS 12.3+), 0 otherwise
 */
int sck_is_available(void);

/**
 * Capture the entire screen as PNG data.
 * @param out_data Pointer to receive allocated buffer (caller must free with sck_free_buffer)
 * @param out_length Pointer to receive buffer length in bytes
 * @return 0 on success, negative error code on failure:
 *         -1: ScreenCaptureKit not available
 *         -2: Permission denied (screen recording not authorized)
 *         -3: Capture failed
 *         -4: PNG encoding failed
 */
int sck_capture_fullscreen(uint8_t** out_data, int* out_length);

/**
 * Capture a rectangular region of the screen as PNG data.
 * @param x Left coordinate of the region
 * @param y Top coordinate of the region  
 * @param w Width of the region
 * @param h Height of the region
 * @param out_data Pointer to receive allocated buffer (caller must free with sck_free_buffer)
 * @param out_length Pointer to receive buffer length in bytes
 * @return 0 on success, negative error code on failure (same as fullscreen)
 */
int sck_capture_rect(float x, float y, float w, float h, uint8_t** out_data, int* out_length);

/**
 * Capture a specific window by window ID as PNG data.
 * @param window_id The CGWindowID of the window to capture
 * @param out_data Pointer to receive allocated buffer (caller must free with sck_free_buffer)
 * @param out_length Pointer to receive buffer length in bytes
 * @return 0 on success, negative error code on failure (same as fullscreen)
 */
int sck_capture_window(uint32_t window_id, uint8_t** out_data, int* out_length);

/**
 * Free a buffer allocated by capture functions.
 * @param data Buffer to free (safe to pass NULL)
 */
void sck_free_buffer(uint8_t* data);

#ifdef __cplusplus
}
#endif

#endif /* SCREENCAPTUREKIT_BRIDGE_H */
