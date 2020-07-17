#ifndef simpleservo_h
#define simpleservo_h

#include <stdarg.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdlib.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef enum {
  Ignored,
  Selected,
  Dismissed_,
} CContextMenuResult;

typedef enum {
  Started,
  Error,
} CDevtoolsServerState;

typedef enum {
  kNone,
  kCharacter,
  kBackspace,
  kDelete,
  kEscape,
  kShift,
  kControl,
  kOptionAlt,
  kCommandWindows,
  kEnter,
  kTab,
  kUpArrow,
  kDownArrow,
  kLeftArrow,
  kRightArrow,
  kHome,
  kEnd,
  kPageUp,
  kPageDown,
} CKeyType;

typedef enum {
  Play = 1,
  Pause,
  SeekBackward,
  SeekForward,
  PreviousTrack,
  NextTrack,
  SkipAd,
  Stop,
  SeekTo,
} CMediaSessionActionType;

typedef enum {
  None = 1,
  Playing,
  Paused,
} CMediaSessionPlaybackState;

typedef enum {
  Left,
  Right,
  Middle,
} CMouseButton;

typedef enum {
  Float,
  Int,
  Str,
  Bool,
  Missing,
} CPrefType;

typedef enum {
  Dismissed,
  Primary,
  Secondary,
} CPromptResult;

typedef struct {
  CPrefType pref_type;
  const char *key;
  const void *value;
  bool is_default;
} CPref;

typedef struct {
  uintptr_t len;
  const CPref *list;
} CPrefList;

/**
 * Servo options
 */
typedef struct {
  const char *args;
  int32_t width;
  int32_t height;
  float density;
  bool enable_subpixel_text_antialiasing;
  const char *const *vslogger_mod_list;
  uint32_t vslogger_mod_size;
  void *native_widget;
  const CPrefList *prefs;
} CInitOptions;

/**
 * Callback used by Servo internals
 */
typedef struct {
  void (*on_load_started)(void);
  void (*on_load_ended)(void);
  void (*on_title_changed)(const char *title);
  bool (*on_allow_navigation)(const char *url);
  void (*on_url_changed)(const char *url);
  void (*on_history_changed)(bool can_go_back, bool can_go_forward);
  void (*on_animating_changed)(bool animating);
  void (*on_shutdown_complete)(void);
  void (*on_ime_state_changed)(bool show);
  const char *(*get_clipboard_contents)(void);
  void (*set_clipboard_contents)(const char *contents);
  void (*on_media_session_metadata)(const char *title, const char *album, const char *artist);
  void (*on_media_session_playback_state_change)(CMediaSessionPlaybackState state);
  void (*on_media_session_set_position_state)(double duration, double position, double playback_rate);
  void (*prompt_alert)(const char *message, bool trusted);
  CPromptResult (*prompt_ok_cancel)(const char *message, bool trusted);
  CPromptResult (*prompt_yes_no)(const char *message, bool trusted);
  const char *(*prompt_input)(const char *message, const char *def, bool trusted);
  void (*on_devtools_started)(CDevtoolsServerState result, unsigned int port);
  void (*show_context_menu)(const char *title, const char *const *items_list, uint32_t items_size);
  void (*on_log_output)(const char *buffer, uint32_t buffer_length);
} CHostCallbacks;

void change_visibility(bool visible);

void click(float x, float y);

void deinit(void);

void fill_gl_texture(uint32_t tex_id, int32_t tex_width, int32_t tex_height);

CPref get_pref(const char *key);

const bool *get_pref_as_bool(const void *ptr);

const double *get_pref_as_float(const void *ptr);

const int64_t *get_pref_as_int(const void *ptr);

const char *get_pref_as_str(const void *ptr);

CPrefList get_prefs(void);

void go_back(void);

void go_forward(void);

void init_with_egl(CInitOptions opts, void (*wakeup)(void), CHostCallbacks callbacks);

void init_with_gl(CInitOptions opts, void (*wakeup)(void), CHostCallbacks callbacks);

bool is_uri_valid(const char *url);

void key_down(uint32_t key_code, CKeyType key_type);

void key_up(uint32_t key_code, CKeyType key_type);

bool load_uri(const char *url);

void media_session_action(CMediaSessionActionType action);

void mouse_down(float x, float y, CMouseButton button);

void mouse_move(float x, float y);

void mouse_up(float x, float y, CMouseButton button);

void on_context_menu_closed(CContextMenuResult result, uint32_t item);

void perform_updates(void);

void pinchzoom(float factor, int32_t x, int32_t y);

void pinchzoom_end(float factor, int32_t x, int32_t y);

void pinchzoom_start(float factor, int32_t x, int32_t y);

void refresh(void);

void register_panic_handler(void (*on_panic)(const char*));

void reload(void);

void request_shutdown(void);

void reset_all_prefs(void);

bool reset_pref(const char *key);

void resize(int32_t width, int32_t height);

void scroll(int32_t dx, int32_t dy, int32_t x, int32_t y);

void scroll_end(int32_t dx, int32_t dy, int32_t x, int32_t y);

void scroll_start(int32_t dx, int32_t dy, int32_t x, int32_t y);

/**
 * The returned string is not freed. This will leak.
 */
const char *servo_version(void);

void set_batch_mode(bool batch);

bool set_bool_pref(const char *key, bool value);

bool set_float_pref(const char *key, double value);

bool set_int_pref(const char *key, int64_t value);

bool set_str_pref(const char *key, const char *value);

void stop(void);

void touch_cancel(float x, float y, int32_t pointer_id);

void touch_down(float x, float y, int32_t pointer_id);

void touch_move(float x, float y, int32_t pointer_id);

void touch_up(float x, float y, int32_t pointer_id);

#ifdef __cplusplus
}
#endif
#endif // !simpleservo_h
