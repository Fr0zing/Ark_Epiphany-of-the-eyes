#pragma once

#include <cstdint>

#ifdef ARK_OVERLAY_NATIVE_EXPORTS
#define ARK_API extern "C" __declspec(dllexport)
#else
#define ARK_API extern "C" __declspec(dllimport)
#endif

#pragma pack(push, 8)
struct ArkNativeActor
{
	std::uint64_t address;
    float x;
    float y;
    float z;
	float velocityX;
	float velocityY;
	float velocityZ;
	std::int32_t kind;
	std::int32_t level;
	std::int32_t relation;
	std::int32_t flags;
	std::int32_t clusterCount;
	std::int32_t visualColor;
	float health;
	float maxHealth;
	std::int32_t boneMask;
	float skeleton[51];
	wchar_t label[96];
	wchar_t weapon[64];
};

struct ArkNativeTransform
{
	std::uint64_t address;
	float x;
	float y;
	float z;
	float velocityX;
	float velocityY;
	float velocityZ;
	std::int32_t boneMask;
	float skeleton[51];
};

struct ArkNativeMotion
{
	std::uint64_t address;
	float x;
	float y;
	float z;
	float velocityX;
	float velocityY;
	float velocityZ;
};

struct ArkNativeStructurePoint
{
	std::uint64_t address;
	float x;
	float y;
	float z;
	std::int32_t relation;
	std::int32_t visualColor;
};

struct ArkNativeNotification
{
	std::int32_t severity;
	std::int32_t ageMs;
	std::int32_t lifetimeMs;
	std::int32_t reserved;
	wchar_t title[48];
	wchar_t text[128];
};

struct ArkNativeCamera
{
    float cameraX;
    float cameraY;
    float cameraZ;
    float pitch;
    float yaw;
    float roll;
    float fov;
    float originX;
    float originY;
    float originZ;
    std::int32_t hasCamera;
    std::int32_t team;
};

struct ArkNativeSettings
{
    std::int32_t enabled;
    std::int32_t miniRadar;
    std::int32_t showBoxes;
    std::int32_t showLevel;
    std::int32_t showDistance;
    std::int32_t showHeight;
    std::int32_t maxLabels;
	std::int32_t miniRadarSize;
	std::int32_t playerBoxStyle;
	std::int32_t showSkeleton;
	std::int32_t showWeapons;
	std::int32_t showHealth;
	std::uint32_t healthColor;
	float healthBarThickness;
	std::uint32_t ownColor;
	std::uint32_t enemyColor;
	std::uint32_t selectedColor;
	std::uint32_t allyColor;
	std::uint32_t deadColor;
	float radarZoomCm;
	std::int32_t autoScale;
	float textSize;
	float textOutline;
	float textOpacity;
	std::int32_t textBackground;
	std::int32_t offscreenArrows;
	std::int32_t threatIndicator;
	float threatDistanceCm;
	std::int32_t showStatuses;
	std::int32_t showMovement;
	std::int32_t avoidLabelOverlap;
	std::int32_t fpsLimit;
	float positionPredictionMs;
	std::int32_t structureLabelMode;
	std::int32_t noglinAlertState;
	float noglinAlertDistanceCm;
	std::int32_t noglinAntidoteSlot;
	std::int32_t noglinAntidoteCount;
	std::int32_t showPlayerTracers;
};
#pragma pack(pop)

ARK_API void* __stdcall ArkOverlay_Create(std::uint32_t gameProcessId);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateActors(void* handle, const ArkNativeActor* actors, std::int32_t count, const ArkNativeSettings* settings);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateTransforms(void* handle, const ArkNativeTransform* transforms, std::int32_t count);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateMotion(void* handle, const ArkNativeTransform* transforms,
	std::int32_t count, const ArkNativeCamera* camera);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateMotionFrame(void* handle, const ArkNativeMotion* motions,
	std::int32_t count, const ArkNativeCamera* camera);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateSkeletons(void* handle, const ArkNativeTransform* transforms, std::int32_t count);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateStructurePoints(void* handle, const ArkNativeStructurePoint* points, std::int32_t count);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateCamera(void* handle, const ArkNativeCamera* camera);
ARK_API std::int32_t __stdcall ArkOverlay_UpdateNotifications(void* handle, const ArkNativeNotification* notifications, std::int32_t count, std::int32_t corner);
ARK_API void __stdcall ArkOverlay_SetEnabled(void* handle, std::int32_t enabled);
ARK_API std::int32_t __stdcall ArkOverlay_GetStats(void* handle, float* renderMilliseconds, float* framesPerSecond);
ARK_API std::int32_t __stdcall ArkOverlay_GetStatsEx(void* handle, float* renderMilliseconds,
	float* peakRenderMilliseconds, float* frameGapMilliseconds, float* peakFrameGapMilliseconds, float* framesPerSecond);
ARK_API void __stdcall ArkOverlay_Destroy(void* handle);
ARK_API std::int32_t __stdcall ArkOverlay_SelfTest();
