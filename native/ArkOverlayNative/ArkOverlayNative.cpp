#define ARK_OVERLAY_NATIVE_EXPORTS
#include "ArkOverlayNative.h"

#include <windows.h>
#include <d3d11_1.h>
#include <dxgi1_2.h>
#include <d2d1_3.h>
#include <d2d1_1helper.h>
#include <dwrite.h>
#include <dcomp.h>
#include <wrl/client.h>

#include <algorithm>
#include <atomic>
#include <chrono>
#include <climits>
#include <cmath>
#include <condition_variable>
#include <cstring>
#include <mutex>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

using Microsoft::WRL::ComPtr;
using namespace std::chrono_literals;

namespace
{
    constexpr wchar_t WindowClassName[] = L"ArkOverlayNativeWindowV1";
    constexpr float Pi = 3.14159265358979323846f;

	struct Vec3 { float x; float y; float z; };

	struct ProjectionContext
	{
		bool valid{};
		Vec3 forward{};
		Vec3 right{};
		Vec3 up{};
		float cameraX{};
		float cameraY{};
		float cameraZ{};
		float halfWidth{};
		float halfHeight{};
		float focal{};
		int width{};
		int height{};
	};

    float Dot(const Vec3& a, const Vec3& b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }

    float Distance(const ArkNativeActor& actor, const ArkNativeCamera& camera)
    {
        const double x = static_cast<double>(actor.x) - camera.originX;
        const double y = static_cast<double>(actor.y) - camera.originY;
        const double z = static_cast<double>(actor.z) - camera.originZ;
        return static_cast<float>(std::sqrt(x * x + y * y + z * z));
    }

	ProjectionContext BuildProjection(const ArkNativeCamera& camera, int width, int height)
	{
		ProjectionContext result{};
		if (!camera.hasCamera || width <= 0 || height <= 0 || camera.fov < 10.0f || camera.fov > 170.0f)
			return result;

		const float pitch = camera.pitch * Pi / 180.0f;
        const float yaw = camera.yaw * Pi / 180.0f;
        const float roll = camera.roll * Pi / 180.0f;
        const float sp = std::sin(pitch), cp = std::cos(pitch);
        const float sy = std::sin(yaw), cy = std::cos(yaw);
        const float sr = std::sin(roll), cr = std::cos(roll);
		result.valid = true;
		result.forward = { cp * cy, cp * sy, sp };
		result.right = { sr * sp * cy - cr * sy, sr * sp * sy + cr * cy, -sr * cp };
		result.up = { -(cr * sp * cy + sr * sy), cy * sr - cr * sp * sy, cr * cp };
		result.cameraX = camera.cameraX;
		result.cameraY = camera.cameraY;
		result.cameraZ = camera.cameraZ;
		result.halfWidth = static_cast<float>(width) * 0.5f;
		result.halfHeight = static_cast<float>(height) * 0.5f;
		result.focal = result.halfWidth / std::tan(camera.fov * Pi / 360.0f);
		result.width = width;
		result.height = height;
		return result;
	}

	bool WorldToScreen(float x, float y, float z, float zOffset,
		const ProjectionContext& projection, D2D1_POINT_2F& screen)
	{
		if (!projection.valid) return false;
		const Vec3 delta{ x - projection.cameraX, y - projection.cameraY, z + zOffset - projection.cameraZ };
		const float depth = Dot(delta, projection.forward);
		if (depth <= 1.0f)
			return false;
		screen.x = projection.halfWidth + Dot(delta, projection.right) * projection.focal / depth;
		screen.y = projection.halfHeight - Dot(delta, projection.up) * projection.focal / depth;
		return std::isfinite(screen.x) && std::isfinite(screen.y) &&
			screen.x >= -100.0f && screen.x <= projection.width + 100.0f &&
			screen.y >= -100.0f && screen.y <= projection.height + 100.0f;
	}

	bool WorldToScreen(const ArkNativeActor& actor, float zOffset,
		const ProjectionContext& projection, D2D1_POINT_2F& screen)
	{
		return WorldToScreen(actor.x, actor.y, actor.z, zOffset, projection, screen);
	}

	bool WorldToScreen(const ArkNativeActor& actor, float zOffset, const ArkNativeCamera& camera,
		int width, int height, D2D1_POINT_2F& screen)
	{
		return WorldToScreen(actor, zOffset, BuildProjection(camera, width, height), screen);
	}

	bool WorldDirectionToScreenEdge(const ArkNativeActor& actor, const ProjectionContext& projection,
		float margin, D2D1_POINT_2F& point, float& angle)
	{
		if (!projection.valid) return false;
		const Vec3 delta{ actor.x - projection.cameraX, actor.y - projection.cameraY, actor.z - projection.cameraZ };
		float horizontal = Dot(delta, projection.right);
		float vertical = -Dot(delta, projection.up);
		if (Dot(delta, projection.forward) < 0.0f) { horizontal = -horizontal; vertical = -vertical; }
		const float length = std::sqrt(horizontal * horizontal + vertical * vertical);
		if (length < 0.001f) { horizontal = 0.0f; vertical = -1.0f; }
		else { horizontal /= length; vertical /= length; }
		const float halfWidth = std::max(1.0f, projection.halfWidth - margin);
		const float halfHeight = std::max(1.0f, projection.halfHeight - margin);
		const float sx = std::abs(horizontal) > 0.0001f ? halfWidth / std::abs(horizontal) : 100000.0f;
		const float syEdge = std::abs(vertical) > 0.0001f ? halfHeight / std::abs(vertical) : 100000.0f;
		const float scale = std::min(sx, syEdge);
		point = D2D1::Point2F(projection.halfWidth + horizontal * scale, projection.halfHeight + vertical * scale);
		angle = std::atan2(vertical, horizontal);
		return true;
	}

	D2D1_COLOR_F ActorColor(std::int32_t kind, float alpha = 1.0f)
    {
        if (kind == 1) return D2D1::ColorF(75.0f / 255.0f, 230.0f / 255.0f, 157.0f / 255.0f, alpha);
		if (kind == 2) return D2D1::ColorF(255.0f / 255.0f, 184.0f / 255.0f, 77.0f / 255.0f, alpha);
		if (kind == 4) return D2D1::ColorF(72.0f / 255.0f, 231.0f / 255.0f, 213.0f / 255.0f, alpha);
		if (kind == 5) return D2D1::ColorF(93.0f / 255.0f, 108.0f / 255.0f, 140.0f / 255.0f, alpha);
		if (kind == 6) return D2D1::ColorF(1.0f, 227.0f / 255.0f, 106.0f / 255.0f, alpha);
		return D2D1::ColorF(88.0f / 255.0f, 199.0f / 255.0f, 255.0f / 255.0f, alpha);
	}

	D2D1_COLOR_F PackedColor(std::uint32_t rgb, float alpha = 1.0f)
	{
		return D2D1::ColorF(
			static_cast<float>((rgb >> 16) & 0xFF) / 255.0f,
			static_cast<float>((rgb >> 8) & 0xFF) / 255.0f,
			static_cast<float>(rgb & 0xFF) / 255.0f, alpha);
	}

	D2D1_COLOR_F ActorVisualColor(const ArkNativeActor& actor, const ArkNativeSettings& settings, float alpha = 1.0f)
	{
		if ((actor.flags & 1) != 0) return PackedColor(settings.deadColor, alpha);
		if (actor.visualColor >= 0) return PackedColor(static_cast<std::uint32_t>(actor.visualColor), alpha);
		if (actor.relation == 1) return PackedColor(settings.ownColor, alpha);
		if (actor.relation == 2) return PackedColor(settings.enemyColor, alpha);
		if (actor.relation == 3) return PackedColor(settings.selectedColor, alpha);
		if (actor.relation == 4) return PackedColor(settings.allyColor, alpha);
		return ActorColor(actor.kind, alpha);
	}

	D2D1_COLOR_F StructurePointColor(const ArkNativeStructurePoint& point, const ArkNativeSettings& settings, float alpha = 1.0f)
	{
		if (point.visualColor >= 0) return PackedColor(static_cast<std::uint32_t>(point.visualColor), alpha);
		if (point.relation == 1) return PackedColor(settings.ownColor, alpha);
		if (point.relation == 2) return PackedColor(settings.enemyColor, alpha);
		if (point.relation == 3) return PackedColor(settings.selectedColor, alpha);
		if (point.relation == 4) return PackedColor(settings.allyColor, alpha);
		return ActorColor(2, alpha);
	}

	D2D1_COLOR_F NotificationColor(std::int32_t severity, float alpha)
	{
		if (severity == 1) return D2D1::ColorF(75.0f / 255.0f, 230.0f / 255.0f, 157.0f / 255.0f, alpha);
		if (severity == 2) return D2D1::ColorF(255.0f / 255.0f, 200.0f / 255.0f, 87.0f / 255.0f, alpha);
		if (severity == 3) return D2D1::ColorF(255.0f / 255.0f, 79.0f / 255.0f, 97.0f / 255.0f, alpha);
		return D2D1::ColorF(88.0f / 255.0f, 199.0f / 255.0f, 255.0f / 255.0f, alpha);
	}

	float NotificationAlpha(const ArkNativeNotification& notification)
	{
		const float lifetime = static_cast<float>(std::max(1, notification.lifetimeMs));
		const float progress = std::clamp(static_cast<float>(std::max(0, notification.ageMs)) / lifetime, 0.0f, 1.0f);
		if (progress < 0.08f) return progress / 0.08f;
		if (progress > 0.82f) return (1.0f - progress) / 0.18f;
		return 1.0f;
	}

    struct WindowSearch
    {
        DWORD processId{};
        HWND result{};
    };

    BOOL CALLBACK FindGameWindow(HWND window, LPARAM parameter)
    {
        auto* search = reinterpret_cast<WindowSearch*>(parameter);
        DWORD processId{};
        GetWindowThreadProcessId(window, &processId);
        if (processId != search->processId || !IsWindowVisible(window) || IsIconic(window))
            return TRUE;
        RECT client{};
        if (!GetClientRect(window, &client) || client.right - client.left < 320 || client.bottom - client.top < 200)
            return TRUE;
        search->result = window;
        return FALSE;
    }

    class OverlayState
    {
    public:
        explicit OverlayState(std::uint32_t processId) : gameProcessId(processId) {}

        bool Start()
        {
            worker = std::thread([this] { ThreadMain(); });
            std::unique_lock lock(readyMutex);
            readyCondition.wait_for(lock, 10s, [this] { return ready; });
            return ready && initialized;
        }

        void Stop()
        {
            stop.store(true, std::memory_order_release);
            HWND localWindow = hwnd.load(std::memory_order_acquire);
            if (localWindow)
                PostMessageW(localWindow, WM_CLOSE, 0, 0);
            if (worker.joinable())
                worker.join();
        }

		void UpdateActors(const ArkNativeActor* source, std::int32_t count, const ArkNativeSettings& value)
		{
			{
				std::lock_guard lock(dataMutex);
				settings = value;
				if (count == 0)
					actors.clear();
				else
				{
					std::vector<ArkNativeActor> updated(source, source + count);
					for (auto& incoming : updated)
					{
						// Both players and dinos receive low-latency root transforms. Keep
						// those newer coordinates when a slower metadata refresh arrives.
						if ((incoming.kind != 1 && incoming.kind != 3) || incoming.address == 0) continue;
						auto currentIndex = actorIndex.find(incoming.address);
						if (currentIndex == actorIndex.end() || currentIndex->second >= actors.size()) continue;
						const ArkNativeActor& current = actors[currentIndex->second];
						if (current.kind != incoming.kind) continue;
						const float deltaX = current.x - incoming.x;
						const float deltaY = current.y - incoming.y;
						const float deltaZ = current.z - incoming.z;
						incoming.x = current.x;
						incoming.y = current.y;
						incoming.z = current.z;
						incoming.velocityX = current.velocityX;
						incoming.velocityY = current.velocityY;
						incoming.velocityZ = current.velocityZ;
						if (incoming.boneMask != 0)
						{
							for (int bone = 0; bone < 17; ++bone)
							{
								incoming.skeleton[bone * 3] += deltaX;
								incoming.skeleton[bone * 3 + 1] += deltaY;
								incoming.skeleton[bone * 3 + 2] += deltaZ;
							}
						}
						else if (current.boneMask != 0)
						{
							// Metadata snapshots intentionally omit skeleton matrices. Keep the
							// newer skeleton supplied by the independent transform stream.
							incoming.boneMask = current.boneMask;
							std::memcpy(incoming.skeleton, current.skeleton, sizeof(incoming.skeleton));
						}
					}
					actors.swap(updated);
				}
				RebuildActorIndex(actors, actorIndex);
				++actorsVersion;
			}
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		bool UpdateTransforms(const ArkNativeTransform* source, std::int32_t count)
		{
			bool changed;
			{
				std::lock_guard lock(dataMutex);
				changed = ApplyTransformsLocked(source, count);
				if (count == 0) motionTransforms.clear();
				else motionTransforms.assign(source, source + count);
				++motionVersion;
			}
			if (changed) dataVersion.fetch_add(1, std::memory_order_release);
			return changed;
		}

		void UpdateStructurePoints(const ArkNativeStructurePoint* source, std::int32_t count)
		{
			{
				std::lock_guard lock(dataMutex);
				if (count == 0) structurePoints.clear();
				else structurePoints.assign(source, source + count);
				++structurePointsVersion;
			}
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		void UpdateCamera(const ArkNativeCamera& value)
        {
			{
				std::lock_guard lock(dataMutex);
				camera = value;
			}
			dataVersion.fetch_add(1, std::memory_order_release);
        }

		void UpdateNotifications(const ArkNativeNotification* source, std::int32_t count, std::int32_t corner)
		{
			{
				std::lock_guard lock(dataMutex);
				notificationCorner = corner;
				if (count == 0) notifications.clear();
				else notifications.assign(source, source + count);
			}
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		void UpdateMotion(const ArkNativeTransform* source, std::int32_t count, const ArkNativeCamera& value)
		{
			{
				std::lock_guard lock(dataMutex);
				ApplyTransformsLocked(source, count);
				if (count == 0) motionTransforms.clear();
				else motionTransforms.assign(source, source + count);
				++motionVersion;
				camera = value;
			}
			// Camera and actor roots become visible to the render thread as one revision,
			// so it can never draw a new camera against the previous transform packet.
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		void UpdateMotionFrame(const ArkNativeMotion* source, std::int32_t count, const ArkNativeCamera& value)
		{
			{
				std::lock_guard lock(dataMutex);
				ApplyMotions(actors, actorIndex, source, count);
				if (count == 0) fastMotions.clear();
				else fastMotions.assign(source, source + count);
				++fastMotionVersion;
				camera = value;
			}
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		void UpdateSkeletons(const ArkNativeTransform* source, std::int32_t count)
		{
			{
				std::lock_guard lock(dataMutex);
				ApplySkeletons(actors, actorIndex, source, count);
				if (count == 0) skeletonTransforms.clear();
				else skeletonTransforms.assign(source, source + count);
				++skeletonVersion;
			}
			dataVersion.fetch_add(1, std::memory_order_release);
		}

		void SetEnabled(bool value)
		{
			const bool previous = enabled.exchange(value, std::memory_order_acq_rel);
			if (previous == value) return;
			// A hidden DirectComposition target can stop signalling its frame-latency
			// object. On the next show, submit one bootstrap frame without waiting for
			// that signal; normal waitable-object pacing resumes after it succeeds.
			if (value) forcePresent.store(true, std::memory_order_release);
			dataVersion.fetch_add(1, std::memory_order_release);
        }

		void GetStats(float& milliseconds, float& fps) const
		{
			milliseconds = renderMilliseconds.load(std::memory_order_relaxed);
			fps = renderFps.load(std::memory_order_relaxed);
		}

		void GetStatsEx(float& milliseconds, float& peakMilliseconds, float& frameGap,
			float& peakFrameGap, float& fps) const
		{
			milliseconds = renderMilliseconds.load(std::memory_order_relaxed);
			peakMilliseconds = renderPeakMilliseconds.load(std::memory_order_relaxed);
			frameGap = frameGapMilliseconds.load(std::memory_order_relaxed);
			peakFrameGap = frameGapPeakMilliseconds.load(std::memory_order_relaxed);
			fps = renderFps.load(std::memory_order_relaxed);
		}

    private:
        std::uint32_t gameProcessId{};
        std::atomic<bool> stop{ false };
        std::atomic<bool> enabled{ true };
		std::atomic<bool> forcePresent{ true };
        std::atomic<HWND> hwnd{};
        std::thread worker;
        std::mutex readyMutex;
        std::condition_variable readyCondition;
        bool ready{};
        bool initialized{};
		std::mutex dataMutex;
		std::vector<ArkNativeActor> actors;
		std::vector<ArkNativeActor> renderActors;
		std::unordered_map<std::uint64_t, std::size_t> actorIndex;
		std::unordered_map<std::uint64_t, std::size_t> renderActorIndex;
		std::vector<ArkNativeTransform> motionTransforms;
		std::vector<ArkNativeTransform> renderMotionTransforms;
		std::vector<ArkNativeMotion> fastMotions;
		std::vector<ArkNativeMotion> renderFastMotions;
		std::vector<ArkNativeTransform> skeletonTransforms;
		std::vector<ArkNativeTransform> renderSkeletonTransforms;
		std::vector<ArkNativeStructurePoint> structurePoints;
		std::vector<ArkNativeStructurePoint> renderStructurePoints;
		std::vector<ArkNativeNotification> notifications;
		std::vector<ArkNativeNotification> renderNotifications;
		std::int32_t notificationCorner{};
		std::vector<D2D1_RECT_F> labelRects;
		std::uint64_t actorsVersion{};
		std::uint64_t renderedActorsVersion{ ~std::uint64_t{} };
		std::uint64_t motionVersion{};
		std::uint64_t renderedMotionVersion{ ~std::uint64_t{} };
		std::uint64_t fastMotionVersion{};
		std::uint64_t renderedFastMotionVersion{ ~std::uint64_t{} };
		std::uint64_t skeletonVersion{};
		std::uint64_t renderedSkeletonVersion{ ~std::uint64_t{} };
		std::uint64_t structurePointsVersion{};
		std::uint64_t renderedStructurePointsVersion{ ~std::uint64_t{} };
		std::atomic<std::uint64_t> dataVersion{ 1 };
		std::atomic<float> renderMilliseconds{ 0.0f };
		std::atomic<float> renderPeakMilliseconds{ 0.0f };
		std::atomic<float> frameGapMilliseconds{ 0.0f };
		std::atomic<float> frameGapPeakMilliseconds{ 0.0f };
		std::atomic<float> renderFps{ 0.0f };
        ArkNativeCamera camera{};
        ArkNativeSettings settings{};
        int renderWidth{};
        int renderHeight{};
        float dpiScale{ 1.0f };

		ComPtr<ID3D11Device> d3dDevice;
		ComPtr<ID3D11DeviceContext> d3dContext;
		ComPtr<IDXGISwapChain1> swapChain;
		ComPtr<IDXGISwapChain2> latencySwapChain;
		HANDLE frameLatencyWaitable{};
		HWND gameWindow{};
		HMONITOR gameMonitor{};
		int alignedX{ INT_MIN };
		int alignedY{ INT_MIN };
		int alignedWidth{};
		int alignedHeight{};
		int displayRefreshHz{};
        ComPtr<ID2D1Factory1> d2dFactory;
        ComPtr<ID2D1Device> d2dDevice;
        ComPtr<ID2D1DeviceContext> d2dContext;
		ComPtr<ID2D1DeviceContext3> d2dContext3;
		ComPtr<ID2D1SpriteBatch> structureSpriteBatch;
		ComPtr<ID2D1SpriteBatch> radarStructureSpriteBatch;
		ComPtr<ID2D1Bitmap1> structureDotBitmap;
		std::vector<D2D1_RECT_F> structureSpriteRects;
		std::vector<D2D1_COLOR_F> structureSpriteColors;
        ComPtr<ID2D1Bitmap1> targetBitmap;
        ComPtr<IDCompositionDevice> compositionDevice;
        ComPtr<IDCompositionTarget> compositionTarget;
        ComPtr<IDCompositionVisual> compositionVisual;
        ComPtr<IDWriteFactory> writeFactory;
        ComPtr<IDWriteTextFormat> labelFormat;
        ComPtr<IDWriteTextFormat> detailFormat;
        ComPtr<ID2D1SolidColorBrush> actorBrush;
        ComPtr<ID2D1SolidColorBrush> whiteBrush;
        ComPtr<ID2D1SolidColorBrush> shadowBrush;
		ComPtr<ID2D1SolidColorBrush> radarBrush;
		ComPtr<ID2D1SolidColorBrush> gridBrush;

		static void RebuildActorIndex(const std::vector<ArkNativeActor>& source,
			std::unordered_map<std::uint64_t, std::size_t>& index)
		{
			index.clear();
			index.reserve(source.size());
			for (std::size_t i = 0; i < source.size(); ++i)
				if (source[i].address != 0) index[source[i].address] = i;
		}

		static bool ApplyTransforms(std::vector<ArkNativeActor>& target,
			const std::unordered_map<std::uint64_t, std::size_t>& index,
			const ArkNativeTransform* source, std::int32_t count)
		{
			bool changed = false;
			for (std::int32_t i = 0; i < count; ++i)
			{
				const ArkNativeTransform& update = source[i];
				if (update.address == 0) continue;
				auto location = index.find(update.address);
				if (location == index.end() || location->second >= target.size()) continue;
				ArkNativeActor* actor = &target[location->second];
				const float deltaX = update.x - actor->x;
				const float deltaY = update.y - actor->y;
				const float deltaZ = update.z - actor->z;
				actor->x = update.x;
				actor->y = update.y;
				actor->z = update.z;
				actor->velocityX = update.velocityX;
				actor->velocityY = update.velocityY;
				actor->velocityZ = update.velocityZ;
				if (update.boneMask >= 0)
				{
					actor->boneMask = update.boneMask;
					std::memcpy(actor->skeleton, update.skeleton, sizeof(actor->skeleton));
				}
				else if (actor->boneMask != 0)
				{
					for (int bone = 0; bone < 17; ++bone)
					{
						actor->skeleton[bone * 3] += deltaX;
						actor->skeleton[bone * 3 + 1] += deltaY;
						actor->skeleton[bone * 3 + 2] += deltaZ;
					}
				}
				changed = true;
			}
			return changed;
		}

		static bool ApplySkeletons(std::vector<ArkNativeActor>& target,
			const std::unordered_map<std::uint64_t, std::size_t>& index,
			const ArkNativeTransform* source, std::int32_t count)
		{
			bool changed = false;
			for (std::int32_t i = 0; i < count; ++i)
			{
				const ArkNativeTransform& update = source[i];
				if (update.address == 0 || update.boneMask < 0) continue;
				auto location = index.find(update.address);
				if (location == index.end() || location->second >= target.size()) continue;
				ArkNativeActor& actor = target[location->second];
				const float deltaX = actor.x - update.x;
				const float deltaY = actor.y - update.y;
				const float deltaZ = actor.z - update.z;
				actor.boneMask = update.boneMask;
				for (int bone = 0; bone < 17; ++bone)
				{
					actor.skeleton[bone * 3] = update.skeleton[bone * 3] + deltaX;
					actor.skeleton[bone * 3 + 1] = update.skeleton[bone * 3 + 1] + deltaY;
					actor.skeleton[bone * 3 + 2] = update.skeleton[bone * 3 + 2] + deltaZ;
				}
				changed = true;
			}
			return changed;
		}

		static bool ApplyMotions(std::vector<ArkNativeActor>& target,
			const std::unordered_map<std::uint64_t, std::size_t>& index,
			const ArkNativeMotion* source, std::int32_t count)
		{
			bool changed = false;
			for (std::int32_t i = 0; i < count; ++i)
			{
				const ArkNativeMotion& update = source[i];
				if (update.address == 0) continue;
				auto location = index.find(update.address);
				if (location == index.end() || location->second >= target.size()) continue;
				ArkNativeActor& actor = target[location->second];
				const float deltaX = update.x - actor.x;
				const float deltaY = update.y - actor.y;
				const float deltaZ = update.z - actor.z;
				actor.x = update.x;
				actor.y = update.y;
				actor.z = update.z;
				actor.velocityX = update.velocityX;
				actor.velocityY = update.velocityY;
				actor.velocityZ = update.velocityZ;
				if (actor.boneMask != 0)
				{
					for (int bone = 0; bone < 17; ++bone)
					{
						actor.skeleton[bone * 3] += deltaX;
						actor.skeleton[bone * 3 + 1] += deltaY;
						actor.skeleton[bone * 3 + 2] += deltaZ;
					}
				}
				changed = true;
			}
			return changed;
		}

		bool ApplyTransformsLocked(const ArkNativeTransform* source, std::int32_t count)
		{
			return ApplyTransforms(actors, actorIndex, source, count);
		}

		static LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM wParam, LPARAM lParam)
        {
            if (message == WM_NCHITTEST) return HTTRANSPARENT;
            if (message == WM_MOUSEACTIVATE) return MA_NOACTIVATE;
            if (message == WM_ERASEBKGND) return 1;
            if (message == WM_CLOSE) { DestroyWindow(window); return 0; }
            if (message == WM_DESTROY) { PostQuitMessage(0); return 0; }
            return DefWindowProcW(window, message, wParam, lParam);
        }

        void SignalReady(bool success)
        {
            std::lock_guard lock(readyMutex);
            initialized = success;
            ready = true;
            readyCondition.notify_all();
        }

        bool CreateWindowForOverlay()
        {
            WNDCLASSEXW cls{ sizeof(cls) };
            cls.lpfnWndProc = WindowProc;
            cls.hInstance = GetModuleHandleW(nullptr);
            cls.lpszClassName = WindowClassName;
            cls.hCursor = LoadCursorW(nullptr, IDC_ARROW);
            RegisterClassExW(&cls);
            // A DirectComposition surface supplies its own transparent content.
            // Without NOREDIRECTIONBITMAP Windows creates a second opaque
            // redirection surface for this layered window, producing a black
            // rectangle over the game on this renderer.
            HWND window = CreateWindowExW(
                WS_EX_TOPMOST | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_NOREDIRECTIONBITMAP,
                WindowClassName, L"ARK Vision Native Overlay", WS_POPUP,
                0, 0, 1, 1, nullptr, nullptr, cls.hInstance, nullptr);
            if (!window) return false;
            // WS_EX_TRANSPARENT alone only affects sibling paint order and
            // HTTRANSPARENT only forwards hit testing within the same thread.
            // A layered + transparent top-level window is required so input
            // reaches ShooterGame.exe, which lives in a different process.
            if (!SetLayeredWindowAttributes(window, 0, 255, LWA_ALPHA))
            {
                DestroyWindow(window);
                return false;
            }
            hwnd.store(window, std::memory_order_release);
            dpiScale = static_cast<float>(GetDpiForWindow(window)) / 96.0f;
            return true;
        }

		bool TryCreateStructureSpriteResources()
		{
			ComPtr<ID2D1DeviceContext3> context3;
			if (FAILED(d2dContext.As(&context3))) return false;
			ComPtr<ID2D1SpriteBatch> spriteBatch;
			if (FAILED(context3->CreateSpriteBatch(&spriteBatch))) return false;
			ComPtr<ID2D1SpriteBatch> radarSpriteBatch;
			if (FAILED(context3->CreateSpriteBatch(&radarSpriteBatch))) return false;

			// A small premultiplied white alpha mask keeps the existing round,
			// antialiased marker while SpriteBatch supplies the color per point.
			constexpr UINT32 textureSize = 32;
			std::vector<std::uint32_t> pixels(textureSize * textureSize);
			const float center = static_cast<float>(textureSize) * 0.5f;
			const float radius = center - 1.0f;
			for (UINT32 y = 0; y < textureSize; ++y)
			{
				for (UINT32 x = 0; x < textureSize; ++x)
				{
					const float dx = static_cast<float>(x) + 0.5f - center;
					const float dy = static_cast<float>(y) + 0.5f - center;
					const float coverage = std::clamp(radius + 0.5f - std::sqrt(dx * dx + dy * dy), 0.0f, 1.0f);
					const auto alpha = static_cast<std::uint32_t>(std::lround(coverage * 255.0f));
					// BGRA8 premultiplied white: every channel equals alpha.
					pixels[y * textureSize + x] = alpha * 0x01010101u;
				}
			}
			const auto properties = D2D1::BitmapProperties1(
				D2D1_BITMAP_OPTIONS_NONE,
				D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED),
				96.0f, 96.0f);
			ComPtr<ID2D1Bitmap1> dotBitmap;
			if (FAILED(context3->CreateBitmap(D2D1::SizeU(textureSize, textureSize), pixels.data(),
				textureSize * sizeof(std::uint32_t), &properties, &dotBitmap))) return false;

			d2dContext3 = std::move(context3);
			structureSpriteBatch = std::move(spriteBatch);
			radarStructureSpriteBatch = std::move(radarSpriteBatch);
			structureDotBitmap = std::move(dotBitmap);
			structureSpriteRects.reserve(10000);
			structureSpriteColors.reserve(10000);
			return true;
		}

        bool CreateGraphics()
        {
            UINT flags = D3D11_CREATE_DEVICE_BGRA_SUPPORT;
#if defined(_DEBUG)
            flags |= D3D11_CREATE_DEVICE_DEBUG;
#endif
            D3D_FEATURE_LEVEL featureLevel{};
            const D3D_FEATURE_LEVEL levels[] = { D3D_FEATURE_LEVEL_11_1, D3D_FEATURE_LEVEL_11_0, D3D_FEATURE_LEVEL_10_1 };
            HRESULT hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_HARDWARE, nullptr, flags,
                levels, ARRAYSIZE(levels), D3D11_SDK_VERSION, &d3dDevice, &featureLevel, &d3dContext);
            if (FAILED(hr))
                hr = D3D11CreateDevice(nullptr, D3D_DRIVER_TYPE_WARP, nullptr, flags,
                    levels, ARRAYSIZE(levels), D3D11_SDK_VERSION, &d3dDevice, &featureLevel, &d3dContext);
            if (FAILED(hr)) return false;

            D2D1_FACTORY_OPTIONS options{};
            hr = D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, __uuidof(ID2D1Factory1),
                &options, reinterpret_cast<void**>(d2dFactory.GetAddressOf()));
            if (FAILED(hr)) return false;
            ComPtr<IDXGIDevice> dxgiDevice;
			if (FAILED(d3dDevice.As(&dxgiDevice))) return false;
			ComPtr<IDXGIDevice1> dxgiDevice1;
			if (SUCCEEDED(dxgiDevice.As(&dxgiDevice1))) dxgiDevice1->SetMaximumFrameLatency(1);
            if (FAILED(d2dFactory->CreateDevice(dxgiDevice.Get(), &d2dDevice))) return false;
            if (FAILED(d2dDevice->CreateDeviceContext(D2D1_DEVICE_CONTEXT_OPTIONS_NONE, &d2dContext))) return false;
			// Sprite batching is intentionally disabled for the stable renderer.  It
			// was an optional optimisation for thousands of structure dots, but some
			// drivers lose the D2D composition target after the first batch.  The
			// regular FillEllipse path is slower only for a very dense base and keeps
			// the overlay, labels and radar reliable.

            ComPtr<IDXGIAdapter> adapter;
            ComPtr<IDXGIFactory2> factory;
            if (FAILED(dxgiDevice->GetAdapter(&adapter)) || FAILED(adapter->GetParent(IID_PPV_ARGS(&factory)))) return false;
            DXGI_SWAP_CHAIN_DESC1 descriptor{};
            descriptor.Width = 1;
            descriptor.Height = 1;
            descriptor.Format = DXGI_FORMAT_B8G8R8A8_UNORM;
            descriptor.SampleDesc.Count = 1;
            descriptor.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
            descriptor.BufferCount = 2;
			descriptor.Scaling = DXGI_SCALING_STRETCH;
			descriptor.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
			descriptor.AlphaMode = DXGI_ALPHA_MODE_PREMULTIPLIED;
			descriptor.Flags = DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT;
			if (FAILED(factory->CreateSwapChainForComposition(d3dDevice.Get(), &descriptor, nullptr, &swapChain))) return false;
			if (SUCCEEDED(swapChain.As(&latencySwapChain)) && SUCCEEDED(latencySwapChain->SetMaximumFrameLatency(1)))
				frameLatencyWaitable = latencySwapChain->GetFrameLatencyWaitableObject();

            if (FAILED(DCompositionCreateDevice(dxgiDevice.Get(), IID_PPV_ARGS(&compositionDevice)))) return false;
            if (FAILED(compositionDevice->CreateTargetForHwnd(hwnd.load(), TRUE, &compositionTarget))) return false;
            if (FAILED(compositionDevice->CreateVisual(&compositionVisual))) return false;
            if (FAILED(compositionVisual->SetContent(swapChain.Get()))) return false;
            if (FAILED(compositionTarget->SetRoot(compositionVisual.Get()))) return false;
            if (FAILED(compositionDevice->Commit())) return false;

            if (FAILED(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), &writeFactory))) return false;
            if (FAILED(writeFactory->CreateTextFormat(L"Segoe UI", nullptr, DWRITE_FONT_WEIGHT_SEMI_BOLD,
                DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, 13.0f * dpiScale, L"ru-RU", &labelFormat))) return false;
            if (FAILED(writeFactory->CreateTextFormat(L"Segoe UI", nullptr, DWRITE_FONT_WEIGHT_NORMAL,
                DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, 11.0f * dpiScale, L"ru-RU", &detailFormat))) return false;
            labelFormat->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);
            detailFormat->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);
            if (FAILED(d2dContext->CreateSolidColorBrush(D2D1::ColorF(D2D1::ColorF::White), &whiteBrush))) return false;
            if (FAILED(d2dContext->CreateSolidColorBrush(D2D1::ColorF(0, 0.82f), &shadowBrush))) return false;
            if (FAILED(d2dContext->CreateSolidColorBrush(D2D1::ColorF(0.04f, 0.075f, 0.105f, 0.90f), &radarBrush))) return false;
            if (FAILED(d2dContext->CreateSolidColorBrush(D2D1::ColorF(0.21f, 0.83f, 0.78f, 0.34f), &gridBrush))) return false;
            if (FAILED(d2dContext->CreateSolidColorBrush(ActorColor(3), &actorBrush))) return false;
            return true;
        }

		bool Resize(int width, int height)
        {
            if (width <= 0 || height <= 0) return false;
            if (width == renderWidth && height == renderHeight && targetBitmap) return true;
            d2dContext->SetTarget(nullptr);
            targetBitmap.Reset();
			const UINT resizeFlags = frameLatencyWaitable ? DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT : 0;
			HRESULT hr = swapChain->ResizeBuffers(0, static_cast<UINT>(width), static_cast<UINT>(height), DXGI_FORMAT_UNKNOWN, resizeFlags);
            if (FAILED(hr)) return false;
            ComPtr<IDXGISurface> surface;
            if (FAILED(swapChain->GetBuffer(0, IID_PPV_ARGS(&surface)))) return false;
            const auto properties = D2D1::BitmapProperties1(
                D2D1_BITMAP_OPTIONS_TARGET | D2D1_BITMAP_OPTIONS_CANNOT_DRAW,
                D2D1::PixelFormat(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_PREMULTIPLIED), 96.0f, 96.0f);
            if (FAILED(d2dContext->CreateBitmapFromDxgiSurface(surface.Get(), &properties, &targetBitmap))) return false;
            d2dContext->SetTarget(targetBitmap.Get());
            renderWidth = width;
            renderHeight = height;
            return true;
        }

		bool AlignToGame()
		{
			DWORD currentProcessId{};
			bool validGameWindow = gameWindow && IsWindow(gameWindow) && IsWindowVisible(gameWindow) && !IsIconic(gameWindow);
			if (validGameWindow)
			{
				GetWindowThreadProcessId(gameWindow, &currentProcessId);
				validGameWindow = currentProcessId == gameProcessId;
			}
			if (!validGameWindow)
			{
				WindowSearch search{ gameProcessId };
				EnumWindows(FindGameWindow, reinterpret_cast<LPARAM>(&search));
				if (!search.result) return false;
				gameWindow = search.result;
				gameMonitor = nullptr;
				alignedX = alignedY = INT_MIN;
				alignedWidth = alignedHeight = 0;
			}
			RECT client{};
			POINT origin{};
			if (!GetClientRect(gameWindow, &client) || !ClientToScreen(gameWindow, &origin)) return false;
			const int width = client.right - client.left;
			const int height = client.bottom - client.top;
			if (width <= 0 || height <= 0) return false;

			const HMONITOR monitor = MonitorFromWindow(gameWindow, MONITOR_DEFAULTTONEAREST);
			if (monitor && monitor != gameMonitor)
			{
				gameMonitor = monitor;
				displayRefreshHz = 0;
				MONITORINFOEXW monitorInfo{};
				monitorInfo.cbSize = sizeof(monitorInfo);
				DEVMODEW mode{};
				mode.dmSize = sizeof(mode);
				if (GetMonitorInfoW(monitor, &monitorInfo) &&
					EnumDisplaySettingsW(monitorInfo.szDevice, ENUM_CURRENT_SETTINGS, &mode) &&
					mode.dmDisplayFrequency >= 24 && mode.dmDisplayFrequency <= 1000)
				{
					displayRefreshHz = static_cast<int>(mode.dmDisplayFrequency);
				}
			}

			const bool moved = origin.x != alignedX || origin.y != alignedY;
			const bool resized = width != alignedWidth || height != alignedHeight;
			if (!moved && !resized) return true;
			HWND window = hwnd.load();
			// The window is created in the topmost band once. Re-inserting it at
			// HWND_TOPMOST on every alignment tick raises it above our control
			// panel and can intercept cross-thread hit testing on Windows 11.
			if (!SetWindowPos(window, nullptr, origin.x, origin.y, width, height,
				SWP_NOACTIVATE | SWP_NOZORDER)) return false;
			if (resized && !Resize(width, height)) return false;
			alignedX = origin.x;
			alignedY = origin.y;
			alignedWidth = width;
			alignedHeight = height;
			return true;
		}

		void ReleaseGraphics()
		{
			if (d2dContext) d2dContext->SetTarget(nullptr);
			structureDotBitmap.Reset();
			radarStructureSpriteBatch.Reset();
			structureSpriteBatch.Reset();
			d2dContext3.Reset();
			targetBitmap.Reset();
			whiteBrush.Reset();
			shadowBrush.Reset();
			radarBrush.Reset();
			gridBrush.Reset();
			actorBrush.Reset();
			labelFormat.Reset();
			detailFormat.Reset();
			writeFactory.Reset();
			compositionVisual.Reset();
			compositionTarget.Reset();
			compositionDevice.Reset();
			frameLatencyWaitable = nullptr;
			latencySwapChain.Reset();
			swapChain.Reset();
			d2dContext.Reset();
			d2dDevice.Reset();
			d2dFactory.Reset();
			d3dContext.Reset();
			d3dDevice.Reset();
			renderWidth = renderHeight = 0;
		}

		bool RecreateGraphics()
		{
			ReleaseGraphics();
			if (!CreateGraphics()) return false;
			if (alignedWidth > 0 && alignedHeight > 0)
				return Resize(alignedWidth, alignedHeight);
			return true;
		}

		void DrawText(const wchar_t* text, IDWriteTextFormat* format, float x, float y, const D2D1_COLOR_F& color,
			float scale = 1.0f, float outline = 1.0f, bool background = false)
        {
            if (!text || !*text) return;
            const UINT32 length = static_cast<UINT32>(wcsnlen_s(text, 512));
			// A turret may supply three compact detail rows.  A taller text layout
			// preserves explicit newlines instead of clipping them after one line.
			const D2D1_RECT_F area = D2D1::RectF(x, y, static_cast<float>(renderWidth), y + 96.0f * dpiScale);
			const float edge = std::max(0.0f, outline) * dpiScale;
			if (background)
				d2dContext->FillRoundedRectangle(D2D1::RoundedRect(D2D1::RectF(x - 3.0f * dpiScale, y - 1.0f * dpiScale,
					std::min(static_cast<float>(renderWidth), x + 250.0f * dpiScale * scale), y + 17.0f * dpiScale * scale), 3.0f, 3.0f), radarBrush.Get());
			if (std::abs(scale - 1.0f) > 0.001f)
				d2dContext->SetTransform(D2D1::Matrix3x2F::Scale(scale, scale, D2D1::Point2F(x, y)));
			if (edge > 0.0f)
			{
				// The previous 4-direction outline (left/right/up/down only) left the
				// diagonal corners of each glyph uncovered, so text over a bright or
				// busy part of the world could still lose contrast at the corners.
				// Adding the four diagonals gives a full ring around every glyph for
				// the same outline thickness the user already configured.
				const float diagonal = edge * 0.7071f;
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left - edge, area.top, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left + edge, area.top, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left, area.top - edge, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left, area.top + edge, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left - diagonal, area.top - diagonal, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left + diagonal, area.top - diagonal, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left - diagonal, area.top + diagonal, area.right, area.bottom), shadowBrush.Get());
				d2dContext->DrawTextW(text, length, format, D2D1::RectF(area.left + diagonal, area.top + diagonal, area.right, area.bottom), shadowBrush.Get());
			}
            actorBrush->SetColor(color);
            d2dContext->DrawTextW(text, length, format, area, actorBrush.Get());
			if (std::abs(scale - 1.0f) > 0.001f)
				d2dContext->SetTransform(D2D1::Matrix3x2F::Identity());
        }

		void DrawOffscreenArrow(const ArkNativeActor& actor, const ProjectionContext& projection, const ArkNativeSettings& settings)
		{
			D2D1_POINT_2F center{};
			float angle{};
			if (!WorldDirectionToScreenEdge(actor, projection, 34.0f * dpiScale, center, angle)) return;
			const float size = 11.0f * dpiScale;
			const D2D1_POINT_2F tip{ center.x + std::cos(angle) * size, center.y + std::sin(angle) * size };
			const D2D1_POINT_2F left{ center.x + std::cos(angle + 2.45f) * size, center.y + std::sin(angle + 2.45f) * size };
			const D2D1_POINT_2F right{ center.x + std::cos(angle - 2.45f) * size, center.y + std::sin(angle - 2.45f) * size };
			actorBrush->SetColor(ActorVisualColor(actor, settings, 0.95f));
			d2dContext->DrawLine(tip, left, actorBrush.Get(), 3.0f * dpiScale);
			d2dContext->DrawLine(tip, right, actorBrush.Get(), 3.0f * dpiScale);
			d2dContext->DrawLine(left, right, actorBrush.Get(), 2.0f * dpiScale);
		}

		float ResolveLabelY(float x, float proposedY, float width, float height, const ArkNativeSettings& settings)
		{
			if (!settings.avoidLabelOverlap) return proposedY;
			float y = proposedY;
			for (int attempt = 0; attempt < 12; ++attempt)
			{
				D2D1_RECT_F candidate = D2D1::RectF(x, y, x + width, y + height);
				bool overlap = false;
				for (const auto& occupied : labelRects)
				{
					if (candidate.left < occupied.right && candidate.right > occupied.left && candidate.top < occupied.bottom && candidate.bottom > occupied.top)
					{
						y = occupied.bottom + 3.0f * dpiScale;
						overlap = true;
						break;
					}
				}
				if (!overlap) break;
			}
			y = std::clamp(y, 2.0f * dpiScale, std::max(2.0f * dpiScale, renderHeight - height - 2.0f * dpiScale));
			labelRects.push_back(D2D1::RectF(x, y, x + width, y + height));
			return y;
		}

		void DrawActor(const ArkNativeActor& actor, const ArkNativeCamera& localCamera, const ArkNativeSettings& localSettings,
			const ProjectionContext& projection, int clusterCount = 1)
		{
			if (clusterCount <= 1) clusterCount = std::max(1, actor.clusterCount);
			D2D1_POINT_2F base{}, top{};
			if (!WorldToScreen(actor, 0.0f, projection, base))
			{
				if (actor.kind == 3 && localSettings.offscreenArrows) DrawOffscreenArrow(actor, projection, localSettings);
				return;
			}
			const float heightWorld = actor.kind == 3 ? 180.0f : (actor.kind == 1 ? 160.0f : 100.0f);
			if (!WorldToScreen(actor, heightWorld, projection, top))
                top = D2D1::Point2F(base.x, base.y - 24.0f * dpiScale);
			const bool isPlayer = actor.kind == 3;
		const bool isDino = actor.kind == 1;
			float boxHeight = std::clamp(std::abs(base.y - top.y), 18.0f * dpiScale, 180.0f * dpiScale);
			float boxWidth = std::max(12.0f * dpiScale, boxHeight * 0.48f);
			D2D1_RECT_F box = D2D1::RectF(base.x - boxWidth * 0.5f, base.y - boxHeight, base.x + boxWidth * 0.5f, base.y);
			if (isPlayer && actor.boneMask != 0)
			{
				float minX = static_cast<float>(renderWidth), minY = static_cast<float>(renderHeight), maxX = 0.0f, maxY = 0.0f;
				int projectedBones = 0;
				for (int bone = 0; bone < 17; ++bone)
				{
					if ((actor.boneMask & (1 << bone)) == 0) continue;
					D2D1_POINT_2F point{};
					if (!WorldToScreen(actor.skeleton[bone * 3], actor.skeleton[bone * 3 + 1], actor.skeleton[bone * 3 + 2],
						0.0f, projection, point)) continue;
					minX = std::min(minX, point.x); minY = std::min(minY, point.y);
					maxX = std::max(maxX, point.x); maxY = std::max(maxY, point.y);
					++projectedBones;
				}
				if (projectedBones >= 4 && maxY > minY)
				{
					const float padX = std::max(4.0f * dpiScale, (maxX - minX) * 0.18f);
					const float padY = std::max(3.0f * dpiScale, (maxY - minY) * 0.08f);
					box = D2D1::RectF(minX - padX, minY - padY, maxX + padX, maxY + padY);
					boxWidth = box.right - box.left;
					boxHeight = box.bottom - box.top;
				}
			}
			if (actor.kind == 2 || actor.kind == 4 || actor.kind == 5 || actor.kind == 6)
			{
				boxWidth = boxHeight = 12.0f * dpiScale;
				box = D2D1::RectF(base.x - 6.0f * dpiScale, base.y - 6.0f * dpiScale,
					base.x + 6.0f * dpiScale, base.y + 6.0f * dpiScale);
			}
			const float distance = Distance(actor, localCamera) / 100.0f;
			const float distanceScale = localSettings.autoScale ? std::clamp(1.15f - distance / 1200.0f, 0.68f, 1.15f) : 1.0f;
			const float textScale = std::clamp(localSettings.textSize / 11.0f, 0.64f, 2.2f) * distanceScale;
			const float lineScale = localSettings.autoScale ? std::clamp(distanceScale, 0.75f, 1.15f) : 1.0f;
			const D2D1_COLOR_F color = ActorVisualColor(actor, localSettings);
			actorBrush->SetColor(color);
			if (isPlayer && localSettings.showPlayerTracers)
			{
				// Anchor at the local player's own on-screen position (origin is
				// already sent every frame for distance calculations) rather than a
				// fixed screen point, since ARK's default camera is third-person and
				// the player's character is usually visible on screen. The C# side
				// falls back to the CAMERA position (copied verbatim, bit-for-bit)
				// whenever the local pawn couldn't be read that frame - detect that
				// exact fallback (origin == camera) and use the fixed bottom-center
				// point instead, same as when projection fails.
				D2D1_POINT_2F anchor{ projection.halfWidth, static_cast<float>(renderHeight) };
				const bool originIsPlayer = localCamera.originX != localCamera.cameraX ||
					localCamera.originY != localCamera.cameraY || localCamera.originZ != localCamera.cameraZ;
				D2D1_POINT_2F localScreen{};
				// zOffset 0 kept drawing from up near the head even with a genuine
				// player origin, so the local root apparently isn't foot-level like
				// remote actors' root is. -160 (a full player height) landed below
				// the feet, -120 was still a bit below - -90 is the current
				// empirical compromise.
				if (originIsPlayer && WorldToScreen(localCamera.originX, localCamera.originY, localCamera.originZ, -90.0f, projection, localScreen))
					anchor = localScreen;
				d2dContext->DrawLine(anchor, base, actorBrush.Get(), 1.2f * dpiScale * lineScale);
			}
			const int boxStyle = isPlayer ? localSettings.playerBoxStyle : (localSettings.showBoxes ? 1 : 0);
			if (actor.kind == 2 || actor.kind == 4 || actor.kind == 5 || actor.kind == 6)
			{
				const float marker = std::clamp(3.0f * dpiScale * lineScale, 2.5f * dpiScale, 5.0f * dpiScale);
				d2dContext->FillEllipse({ base, marker, marker }, actorBrush.Get());
				 d2dContext->DrawEllipse({ base, marker + 1.5f * dpiScale, marker + 1.5f * dpiScale },
					shadowBrush.Get(), 1.0f * dpiScale);
			}
			// Possible spawn locations are navigation pins only. They deliberately
			// have no label or distance line, otherwise a full route becomes text
			// clutter and looks like confirmed loot.
			if (actor.kind == 5) return;
			else if (boxStyle == 2)
			{
				d2dContext->DrawRectangle(box, actorBrush.Get(), 1.5f * dpiScale * lineScale);
			}
			else if (boxStyle == 1)
			{
				const float x = boxWidth * 0.28f, y = boxHeight * 0.22f, stroke = 1.7f * dpiScale * lineScale;
                d2dContext->DrawLine({ box.left, box.top }, { box.left + x, box.top }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.left, box.top }, { box.left, box.top + y }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.right, box.top }, { box.right - x, box.top }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.right, box.top }, { box.right, box.top + y }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.left, box.bottom }, { box.left + x, box.bottom }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.left, box.bottom }, { box.left, box.bottom - y }, actorBrush.Get(), stroke);
                d2dContext->DrawLine({ box.right, box.bottom }, { box.right - x, box.bottom }, actorBrush.Get(), stroke);
				d2dContext->DrawLine({ box.right, box.bottom }, { box.right, box.bottom - y }, actorBrush.Get(), stroke);
			}
			if (isPlayer && localSettings.showSkeleton)
			{
				const int links[][2] = { {0,1}, {1,2}, {2,3}, {3,4}, {2,5}, {5,6}, {6,7}, {2,8}, {8,9}, {9,10}, {0,11}, {11,12}, {12,13}, {0,14}, {14,15}, {15,16} };
				bool drewRealSkeleton = false;
				for (const auto& link : links)
				{
					const int first = link[0], second = link[1];
					if ((actor.boneMask & (1 << first)) == 0 || (actor.boneMask & (1 << second)) == 0) continue;
					D2D1_POINT_2F a{}, b{};
					if (WorldToScreen(actor.skeleton[first * 3], actor.skeleton[first * 3 + 1], actor.skeleton[first * 3 + 2], 0.0f, projection, a) &&
						WorldToScreen(actor.skeleton[second * 3], actor.skeleton[second * 3 + 1], actor.skeleton[second * 3 + 2], 0.0f, projection, b))
					{
						d2dContext->DrawLine(a, b, actorBrush.Get(), 1.35f * dpiScale * lineScale);
						drewRealSkeleton = true;
					}
				}
				if (drewRealSkeleton && (actor.boneMask & (1 << 4)) != 0)
				{
					D2D1_POINT_2F head{};
					if (WorldToScreen(actor.skeleton[12], actor.skeleton[13], actor.skeleton[14], 0.0f, projection, head))
						d2dContext->DrawEllipse({ head, std::max(2.5f * dpiScale, boxWidth * 0.08f), std::max(3.0f * dpiScale, boxWidth * 0.10f) }, actorBrush.Get(), 1.2f * dpiScale * lineScale);
				}
				if (!drewRealSkeleton)
				{
					const float center = (box.left + box.right) * 0.5f;
					const float chestY = box.top + boxHeight * 0.42f, hipY = box.top + boxHeight * 0.64f;
					d2dContext->DrawEllipse({ { center, box.top + boxHeight * 0.12f }, boxWidth * 0.10f, boxWidth * 0.12f }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
					d2dContext->DrawLine({ center, box.top + boxHeight * 0.24f }, { center, hipY }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
					d2dContext->DrawLine({ center, chestY }, { box.left + boxWidth * 0.10f, box.top + boxHeight * 0.55f }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
					d2dContext->DrawLine({ center, chestY }, { box.right - boxWidth * 0.10f, box.top + boxHeight * 0.55f }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
					d2dContext->DrawLine({ center, hipY }, { box.left + boxWidth * 0.20f, box.bottom }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
					d2dContext->DrawLine({ center, hipY }, { box.right - boxWidth * 0.20f, box.bottom }, actorBrush.Get(), 1.25f * dpiScale * lineScale);
				}
			}
			if (isPlayer && localSettings.showMovement)
			{
				const float speed = std::sqrt(actor.velocityX * actor.velocityX + actor.velocityY * actor.velocityY);
				if (speed > 25.0f)
				{
					D2D1_POINT_2F from{}, to{};
					if (WorldToScreen(actor, 90.0f, projection, from) &&
						WorldToScreen(actor.x + actor.velocityX / speed * 150.0f,
							actor.y + actor.velocityY / speed * 150.0f, actor.z, 90.0f, projection, to))
					{
						actorBrush->SetColor(color);
						d2dContext->DrawLine(from, to, actorBrush.Get(), 2.0f * dpiScale * lineScale);
						d2dContext->FillEllipse({ to, 2.5f * dpiScale, 2.5f * dpiScale }, actorBrush.Get());
					}
				}
			}
			// Bit 32 in flags is a per-actor "show health" decision computed on the
			// C# side (player vs dino use independent toggles), not a single global
			// switch - lets both kinds have their own setting without growing the
			// native settings ABI.
			const bool showHealthForActor = (actor.flags & 32) != 0;
			if (showHealthForActor && actor.maxHealth > 0.0f)
			{
				const float ratio = std::clamp(actor.health / actor.maxHealth, 0.0f, 1.0f);
				const float thickness = std::clamp(localSettings.healthBarThickness, 1.0f, 14.0f) * dpiScale;
				const float right = box.left - 4.0f * dpiScale;
				const D2D1_RECT_F background = D2D1::RectF(right - thickness - dpiScale, box.top - dpiScale, right + dpiScale, box.bottom + dpiScale);
				d2dContext->FillRectangle(background, shadowBrush.Get());
				const D2D1_COLOR_F healthColor = PackedColor(localSettings.healthColor);
				actorBrush->SetColor(healthColor);
				const float filledTop = box.bottom - boxHeight * ratio;
				d2dContext->FillRectangle(D2D1::RectF(right - thickness, filledTop, right, box.bottom), actorBrush.Get());
				actorBrush->SetColor(color);
			}
			if (actor.kind == 2 && (actor.flags & 16) == 0) return;
			// Bit 64: per-actor "show full label" decision for dinos, computed on the
			// C# side from the priority-species list (empty list = everyone gets full
			// detail, same as before this existed). Lets an uninteresting species stay
			// a plain box/dot - name, level, distance, HP and status all skipped -
			// without a native species list or a new ABI field.
			if (actor.kind == 1 && (actor.flags & 64) == 0) return;
			const float labelX = box.right + 6.0f * dpiScale;
			const bool turretColumnLabel = (actor.flags & 128) != 0;
			const bool striderModuleLabel = (actor.flags & 256) != 0;
			const float estimatedLines = turretColumnLabel ? 5.0f : 2.0f + (isPlayer && localSettings.showWeapons && actor.weapon[0] ? 1.0f : 0.0f) +
				(striderModuleLabel && actor.weapon[0] ? 1.0f : 0.0f) +
				(showHealthForActor && actor.maxHealth > 0.0f ? 1.0f : 0.0f) + ((isPlayer || isDino) && localSettings.showStatuses ? 1.0f : 0.0f);
			const float labelY = ResolveLabelY(labelX, box.top, 250.0f * dpiScale * textScale,
				estimatedLines * 17.0f * dpiScale * textScale, localSettings);
			const float lineStep = 16.0f * dpiScale * textScale;
			const float outline = std::clamp(localSettings.textOutline, 0.0f, 5.0f);
			const float opacity = std::clamp(localSettings.textOpacity, 0.2f, 1.0f);
			const bool textBackground = localSettings.textBackground != 0;
            wchar_t title[160]{};
			if (clusterCount > 1 && localSettings.showLevel && actor.level > 0)
				swprintf_s(title, L"%s  ×%d  ·  ур. %d", actor.label, clusterCount, actor.level);
			else if (clusterCount > 1)
				swprintf_s(title, L"%s  ×%d", actor.label, clusterCount);
			else if (localSettings.showLevel && actor.level > 0)
                swprintf_s(title, L"%s  ур. %d", actor.label, actor.level);
            else
                wcsncpy_s(title, actor.label, _TRUNCATE);
			D2D1_COLOR_F titleColor = color;
			titleColor.a = opacity;
			DrawText(title, labelFormat.Get(), labelX, labelY, titleColor, textScale, outline, textBackground);
			if (turretColumnLabel)
			{
				// Weapon is repurposed for structures as a three-row telemetry block:
				// mode, configured radius, and live magazine. Distance is always
				// local to the player and completes the five-line turret label.
				DrawText(actor.weapon, detailFormat.Get(), labelX, labelY + lineStep,
					D2D1::ColorF(0.90f, 0.94f, 0.97f, opacity), textScale, outline, textBackground);
				wchar_t turretDistance[64]{};
				swprintf_s(turretDistance, L"%.0f м", distance);
				DrawText(turretDistance, detailFormat.Get(), labelX, labelY + lineStep * 4.0f,
					D2D1::ColorF(0.90f, 0.94f, 0.97f, opacity), textScale, outline, textBackground);
				return;
			}
            wchar_t detail[128]{};
            const float height = (actor.z - localCamera.originZ) / 100.0f;
            if (localSettings.showDistance && localSettings.showHeight)
                swprintf_s(detail, L"%.0f м  ·  высота %+.0f м", distance, height);
            else if (localSettings.showDistance)
                swprintf_s(detail, L"%.0f м", distance);
            else if (localSettings.showHeight)
                swprintf_s(detail, L"высота %+.0f м", height);
			DrawText(detail, detailFormat.Get(), labelX, labelY + lineStep,
				D2D1::ColorF(0.90f, 0.94f, 0.97f, opacity), textScale, outline, textBackground);
			float nextLine = labelY + lineStep * 2.0f;
			if (isPlayer && localSettings.showWeapons && actor.weapon[0])
			{
				DrawText(actor.weapon, detailFormat.Get(), labelX, nextLine,
					D2D1::ColorF(0.73f, 0.78f, 0.86f, opacity), textScale, outline, textBackground);
				nextLine += lineStep;
			}
			if (striderModuleLabel && actor.weapon[0])
			{
				wchar_t modules[96]{};
				swprintf_s(modules, L"Риги: %s", actor.weapon);
				DrawText(modules, detailFormat.Get(), labelX, nextLine,
					D2D1::ColorF(0.37f, 0.86f, 1.0f, opacity), textScale, outline, textBackground);
				nextLine += lineStep;
			}
			if (showHealthForActor && actor.maxHealth > 0.0f)
			{
				wchar_t healthText[64]{};
				swprintf_s(healthText, L"HP %.0f / %.0f", std::max(0.0f, actor.health), actor.maxHealth);
				D2D1_COLOR_F healthColor = PackedColor(localSettings.healthColor, opacity);
				DrawText(healthText, detailFormat.Get(), labelX, nextLine, healthColor, textScale, outline, textBackground);
				nextLine += lineStep;
			}
			if (isPlayer && localSettings.showStatuses)
			{
				wchar_t status[96]{};
				if ((actor.flags & 1) != 0) wcscat_s(status, L"погиб");
				else if ((actor.flags & 2) != 0) wcscat_s(status, L"спит");
				else if ((actor.flags & 4) != 0) wcscat_s(status, L"движется");
				else wcscat_s(status, L"неподвижен");
				if ((actor.flags & 8) != 0) wcscat_s(status, L"  ·  верхом");
				DrawText(status, detailFormat.Get(), labelX, nextLine, D2D1::ColorF(0.98f, 0.83f, 0.38f, opacity),
					textScale, outline, textBackground);
			}
        }

		void DrawThreatIndicator(const std::vector<ArkNativeActor>& localActors, const ArkNativeCamera& camera, const ArkNativeSettings& settings)
		{
			if (!settings.threatIndicator || settings.threatDistanceCm <= 0.0f) return;
			int count = 0;
			float nearest = settings.threatDistanceCm;
			for (const auto& actor : localActors)
			{
				if (actor.kind != 3 || actor.relation != 2 || (actor.flags & 1) != 0) continue;
				const float distanceCm = Distance(actor, camera);
				if (distanceCm <= settings.threatDistanceCm)
				{
					++count;
					nearest = std::min(nearest, distanceCm);
				}
			}
			if (count == 0) return;
			wchar_t text[96]{};
			swprintf_s(text, L"⚠ рядом врагов: %d  ·  ближайший %.0f м", count, nearest / 100.0f);
			const float x = std::max(8.0f * dpiScale, renderWidth * 0.5f - 150.0f * dpiScale);
			DrawText(text, labelFormat.Get(), x, 18.0f * dpiScale, PackedColor(settings.enemyColor),
				std::clamp(settings.textSize / 11.0f, 0.75f, 1.8f), std::max(1.0f, settings.textOutline), true);
		}

		void DrawNoglinAlert(const ArkNativeSettings& settings)
		{
			if (settings.noglinAlertState <= 0) return;
			wchar_t text[160]{};
			D2D1_COLOR_F color = D2D1::ColorF(0.76f, 0.38f, 1.0f, 1.0f);
			const float metres = std::max(0.0f, settings.noglinAlertDistanceCm / 100.0f);
			if (settings.noglinAlertState == 2)
			{
				swprintf_s(text, L"ВРАЖЕСКИЙ НОГЛИН — %.0f м  ·  АНТИДОТА НЕТ", metres);
				color = D2D1::ColorF(1.0f, 0.23f, 0.31f, 1.0f);
			}
			else if (settings.noglinAlertState == 5)
			{
				swprintf_s(text, L"ВРАЖЕСКИЙ НОГЛИН — %.0f м  ·  антидот использован  ·  БОЛЬШЕ НЕТ", metres);
				color = D2D1::ColorF(1.0f, 0.23f, 0.31f, 1.0f);
			}
			else if (settings.noglinAlertState == 3)
			{
				swprintf_s(text, L"ВРАЖЕСКИЙ НОГЛИН — %.0f м  ·  антидот использован  ·  осталось %d", metres, std::max(0, settings.noglinAntidoteCount));
				color = D2D1::ColorF(0.25f, 0.95f, 0.62f, 1.0f);
			}
			else if (settings.noglinAlertState == 4)
			{
				swprintf_s(text, L"ВРАЖЕСКИЙ НОГЛИН — %.0f м  ·  вернитесь в ARK  ·  антидот: слот %d", metres, settings.noglinAntidoteSlot);
				color = D2D1::ColorF(1.0f, 0.71f, 0.22f, 1.0f);
			}
			else
			{
				swprintf_s(text, L"ВРАЖЕСКИЙ НОГЛИН — %.0f м  ·  антидот: слот %d  ×%d", metres, settings.noglinAntidoteSlot, std::max(0, settings.noglinAntidoteCount));
			}
			const float x = std::max(8.0f * dpiScale, renderWidth * 0.5f - 300.0f * dpiScale);
			DrawText(text, labelFormat.Get(), x, 58.0f * dpiScale, color,
				std::clamp(settings.textSize / 10.0f, 0.9f, 2.0f), std::max(1.5f, settings.textOutline), true);
		}

		void DrawNotifications(const std::vector<ArkNativeNotification>& feed, std::int32_t corner)
		{
			if (feed.empty()) return;
			const float margin = 20.0f * dpiScale;
			const float gap = 8.0f * dpiScale;
			const float panelWidth = std::clamp(static_cast<float>(renderWidth) * 0.32f, 240.0f * dpiScale, 390.0f * dpiScale);
			const float panelHeight = 58.0f * dpiScale;
			const bool toRight = corner == 1 || corner == 3;
			const bool toBottom = corner == 2 || corner == 3;
			float cursor = toBottom ? static_cast<float>(renderHeight) - margin : margin;
			for (const auto& notification : feed)
			{
				const float alpha = NotificationAlpha(notification);
				if (alpha <= 0.01f) continue;
				const float left = toRight ? static_cast<float>(renderWidth) - margin - panelWidth : margin;
				const float top = toBottom ? cursor - panelHeight : cursor;
				if (top < -panelHeight || top > static_cast<float>(renderHeight)) break;
				radarBrush->SetColor(D2D1::ColorF(0.05f, 0.06f, 0.10f, 0.88f * alpha));
				d2dContext->FillRoundedRectangle(D2D1::RoundedRect(
					D2D1::RectF(left, top, left + panelWidth, top + panelHeight), 4.0f * dpiScale, 4.0f * dpiScale), radarBrush.Get());
				const D2D1_COLOR_F accent = NotificationColor(notification.severity, alpha);
				actorBrush->SetColor(accent);
				d2dContext->FillRectangle(D2D1::RectF(left, top, left + 4.0f * dpiScale, top + panelHeight), actorBrush.Get());
				DrawText(notification.title, labelFormat.Get(), left + 14.0f * dpiScale, top + 7.0f * dpiScale, accent, 1.0f, 0.0f, false);
				DrawText(notification.text, detailFormat.Get(), left + 14.0f * dpiScale, top + 31.0f * dpiScale,
					D2D1::ColorF(0.90f, 0.93f, 0.97f, alpha), 1.0f, 0.0f, false);
				cursor = toBottom ? top - gap : top + panelHeight + gap;
			}
			radarBrush->SetColor(D2D1::ColorF(0.04f, 0.075f, 0.105f, 0.90f));
		}

		void DrawStructurePoint(const ArkNativeStructurePoint& point, const ArkNativeSettings& localSettings,
			const ProjectionContext& projection)
		{
			D2D1_POINT_2F screen{};
			if (!WorldToScreen(point.x, point.y, point.z, 0.0f, projection, screen)) return;
			actorBrush->SetColor(StructurePointColor(point, localSettings));
			const float marker = 3.0f * dpiScale;
			d2dContext->FillEllipse({ screen, marker, marker }, actorBrush.Get());
		}

		bool SubmitStructureSprites(ID2D1SpriteBatch* spriteBatch)
		{
			if (!d2dContext3 || !spriteBatch || !structureDotBitmap) return false;
			const auto count = static_cast<UINT32>(structureSpriteRects.size());
			if (count == 0)
			{
				spriteBatch->Clear();
				return true;
			}

			HRESULT result{};
			if (spriteBatch->GetSpriteCount() == count)
			{
				result = spriteBatch->SetSprites(0, count,
					structureSpriteRects.data(), nullptr, structureSpriteColors.data(), nullptr);
			}
			else
			{
				spriteBatch->Clear();
				result = spriteBatch->AddSprites(count,
					structureSpriteRects.data(), nullptr, structureSpriteColors.data(), nullptr);
			}
			if (FAILED(result))
			{
				spriteBatch->Clear();
				return false;
			}

			d2dContext3->DrawSpriteBatch(spriteBatch, structureDotBitmap.Get(),
				D2D1_BITMAP_INTERPOLATION_MODE_LINEAR, D2D1_SPRITE_OPTIONS_CLAMP_TO_SOURCE_RECTANGLE);
			return true;
		}

		bool DrawStructurePointsBatched(const std::vector<ArkNativeStructurePoint>& points,
			const ArkNativeSettings& localSettings, const ProjectionContext& projection)
		{
			if (!d2dContext3 || !structureSpriteBatch || !structureDotBitmap) return false;
			structureSpriteRects.clear();
			structureSpriteColors.clear();
			const float marker = 3.0f * dpiScale;
			for (const auto& point : points)
			{
				D2D1_POINT_2F screen{};
				if (!WorldToScreen(point.x, point.y, point.z, 0.0f, projection, screen)) continue;
				structureSpriteRects.push_back(D2D1::RectF(
					screen.x - marker, screen.y - marker, screen.x + marker, screen.y + marker));
				structureSpriteColors.push_back(StructurePointColor(point, localSettings));
			}
			return SubmitStructureSprites(structureSpriteBatch.Get());
		}

		bool DrawRadarStructurePointsBatched(const std::vector<ArkNativeStructurePoint>& points,
			const ArkNativeCamera& localCamera, const ArkNativeSettings& localSettings,
			const D2D1_ELLIPSE& outer, float size, float cosine, float sine, float scale)
		{
			if (!d2dContext3 || !radarStructureSpriteBatch || !structureDotBitmap) return false;
			structureSpriteRects.clear();
			structureSpriteColors.clear();
			const float marker = 2.5f * dpiScale;
			const float radiusSquared = size * size * 0.47f * 0.47f;
			for (const auto& structure : points)
			{
				const float dx = structure.x - localCamera.originX;
				const float dy = structure.y - localCamera.originY;
				const float forward = dx * cosine + dy * sine;
				const float side = -dx * sine + dy * cosine;
				const D2D1_POINT_2F point{ outer.point.x + side * scale, outer.point.y - forward * scale };
				const float px = point.x - outer.point.x;
				const float py = point.y - outer.point.y;
				if (px * px + py * py > radiusSquared) continue;
				structureSpriteRects.push_back(D2D1::RectF(
					point.x - marker, point.y - marker, point.x + marker, point.y + marker));
				structureSpriteColors.push_back(StructurePointColor(structure, localSettings));
			}
			return SubmitStructureSprites(radarStructureSpriteBatch.Get());
		}

		void DrawRadar(const std::vector<ArkNativeActor>& localActors, const std::vector<ArkNativeStructurePoint>& localPoints,
			const ArkNativeCamera& localCamera, const ArkNativeSettings& localSettings)
        {
            const float size = std::min(static_cast<float>(localSettings.miniRadarSize) * dpiScale,
                static_cast<float>(std::min(renderWidth, renderHeight)) * 0.45f);
            const float left = renderWidth - size - 20.0f * dpiScale;
            const float top = 20.0f * dpiScale;
            const D2D1_ELLIPSE outer{ { left + size * 0.5f, top + size * 0.5f }, size * 0.5f, size * 0.5f };
            d2dContext->FillEllipse(outer, radarBrush.Get());
            d2dContext->DrawEllipse(outer, gridBrush.Get(), dpiScale);
            d2dContext->DrawEllipse({ outer.point, size * 0.25f, size * 0.25f }, gridBrush.Get(), dpiScale);
            d2dContext->DrawLine({ outer.point.x, top }, { outer.point.x, top + size }, gridBrush.Get(), dpiScale);
            d2dContext->DrawLine({ left, outer.point.y }, { left + size, outer.point.y }, gridBrush.Get(), dpiScale);
            const float heading = localCamera.yaw * Pi / 180.0f;
            const float c = std::cos(heading), s = std::sin(heading);
            const float scale = size * 0.47f / std::max(1.0f, localSettings.radarZoomCm);
			for (const auto& actor : localActors)
            {
                const float dx = actor.x - localCamera.originX, dy = actor.y - localCamera.originY;
                const float forward = dx * c + dy * s;
                const float side = -dx * s + dy * c;
                const D2D1_POINT_2F point{ outer.point.x + side * scale, outer.point.y - forward * scale };
                const float px = point.x - outer.point.x, py = point.y - outer.point.y;
                if (px * px + py * py > size * size * 0.47f * 0.47f) continue;
				actorBrush->SetColor(ActorVisualColor(actor, localSettings));
				d2dContext->FillEllipse({ point, 3.0f * dpiScale, 3.0f * dpiScale }, actorBrush.Get());
			}
			if (!DrawRadarStructurePointsBatched(localPoints, localCamera, localSettings, outer, size, c, s, scale))
			{
				for (const auto& structure : localPoints)
				{
					const float dx = structure.x - localCamera.originX, dy = structure.y - localCamera.originY;
					const float forward = dx * c + dy * s;
					const float side = -dx * s + dy * c;
					const D2D1_POINT_2F point{ outer.point.x + side * scale, outer.point.y - forward * scale };
					const float px = point.x - outer.point.x, py = point.y - outer.point.y;
					if (px * px + py * py > size * size * 0.47f * 0.47f) continue;
					actorBrush->SetColor(StructurePointColor(structure, localSettings));
					d2dContext->FillEllipse({ point, 2.5f * dpiScale, 2.5f * dpiScale }, actorBrush.Get());
				}
			}
            whiteBrush->SetColor(D2D1::ColorF(D2D1::ColorF::White));
            d2dContext->FillEllipse({ outer.point, 4.0f * dpiScale, 4.0f * dpiScale }, whiteBrush.Get());
        }

		HRESULT Render()
		{
			if (!targetBitmap || !enabled.load(std::memory_order_acquire)) return E_FAIL;
			ArkNativeCamera localCamera{};
            ArkNativeSettings localSettings{};
			std::int32_t localNotificationCorner{};
			bool motionChanged = false;
			bool fastMotionChanged = false;
			bool skeletonChanged = false;
            {
                std::lock_guard lock(dataMutex);
				if (renderedActorsVersion != actorsVersion)
				{
					renderActors = actors;
					RebuildActorIndex(renderActors, renderActorIndex);
					renderedActorsVersion = actorsVersion;
				}
				if (renderedMotionVersion != motionVersion)
				{
					renderMotionTransforms = motionTransforms;
					renderedMotionVersion = motionVersion;
					motionChanged = true;
				}
				if (renderedFastMotionVersion != fastMotionVersion)
				{
					renderFastMotions = fastMotions;
					renderedFastMotionVersion = fastMotionVersion;
					fastMotionChanged = true;
				}
				if (renderedSkeletonVersion != skeletonVersion)
				{
					renderSkeletonTransforms = skeletonTransforms;
					renderedSkeletonVersion = skeletonVersion;
					skeletonChanged = true;
				}
				if (renderedStructurePointsVersion != structurePointsVersion)
				{
					renderStructurePoints = structurePoints;
					renderedStructurePointsVersion = structurePointsVersion;
				}
                localCamera = camera;
                localSettings = settings;
				renderNotifications = notifications;
				localNotificationCorner = notificationCorner;
            }
			// Only the small dynamic transform packet changes at 200+ Hz. Applying it
			// to the render-thread copy avoids copying every label/structure actor on
			// each camera sample.
			if (motionChanged && !renderMotionTransforms.empty())
				ApplyTransforms(renderActors, renderActorIndex, renderMotionTransforms.data(),
					static_cast<std::int32_t>(renderMotionTransforms.size()));
			if (fastMotionChanged && !renderFastMotions.empty())
				ApplyMotions(renderActors, renderActorIndex, renderFastMotions.data(),
					static_cast<std::int32_t>(renderFastMotions.size()));
			if (skeletonChanged && !renderSkeletonTransforms.empty())
				ApplySkeletons(renderActors, renderActorIndex, renderSkeletonTransforms.data(),
					static_cast<std::int32_t>(renderSkeletonTransforms.size()));
            d2dContext->BeginDraw();
			d2dContext->Clear(D2D1::ColorF(0, 0.0f));
			labelRects.clear();
			if (localCamera.hasCamera)
			{
				const ProjectionContext projection = BuildProjection(localCamera, renderWidth, renderHeight);
				if (!DrawStructurePointsBatched(renderStructurePoints, localSettings, projection))
					for (const auto& point : renderStructurePoints) DrawStructurePoint(point, localSettings, projection);
				for (auto it = renderActors.rbegin(); it != renderActors.rend(); ++it)
				{
					if (it->kind == 3 && localSettings.positionPredictionMs > 0.0f)
					{
						ArkNativeActor predicted = *it;
						const float seconds = std::clamp(localSettings.positionPredictionMs, 0.0f, 50.0f) / 1000.0f;
						const float deltaX = predicted.velocityX * seconds;
						const float deltaY = predicted.velocityY * seconds;
						const float deltaZ = predicted.velocityZ * seconds;
						predicted.x += deltaX; predicted.y += deltaY; predicted.z += deltaZ;
						if (predicted.boneMask != 0)
						{
							for (int bone = 0; bone < 17; ++bone)
							{
								predicted.skeleton[bone * 3] += deltaX;
								predicted.skeleton[bone * 3 + 1] += deltaY;
								predicted.skeleton[bone * 3 + 2] += deltaZ;
							}
						}
						DrawActor(predicted, localCamera, localSettings, projection);
					}
					else DrawActor(*it, localCamera, localSettings, projection);
				}
				DrawThreatIndicator(renderActors, localCamera, localSettings);
				DrawNoglinAlert(localSettings);
				if (localSettings.miniRadar)
					DrawRadar(renderActors, renderStructurePoints, localCamera, localSettings);
            }
			DrawNotifications(renderNotifications, localNotificationCorner);
			const HRESULT result = d2dContext->EndDraw();
			if (FAILED(result)) return result;
			// The frame-latency object guarantees an available flip slot. Presenting
			// without DO_NOT_WAIT avoids silently discarding an otherwise complete frame.
			return swapChain->Present(0, 0);
		}

		void ThreadMain()
		{
			if (!CreateWindowForOverlay() || !CreateGraphics())
            {
                SignalReady(false);
                return;
			}
			SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_ABOVE_NORMAL);
			SignalReady(true);
			auto nextAlignment = std::chrono::steady_clock::now();
			auto lastFrameStartedAt = nextAlignment - 1s;
			auto fpsWindowStart = nextAlignment;
			auto previousSuccessfulFrameStart = nextAlignment;
			int framesInWindow = 0;
			float peakRenderInWindow = 0.0f;
			float peakFrameGapInWindow = 0.0f;
			bool hasPreviousSuccessfulFrame = false;
			bool frameSlotReady = true;
			bool windowShown = false;
			std::uint64_t renderedVersion{};
			MSG message{};
            while (!stop.load(std::memory_order_acquire))
            {
                while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE))
                {
                    if (message.message == WM_QUIT) { stop.store(true); break; }
                    TranslateMessage(&message);
                    DispatchMessageW(&message);
                }
				const auto alignmentNow = std::chrono::steady_clock::now();
				if (alignmentNow >= nextAlignment)
				{
					AlignToGame();
					nextAlignment = alignmentNow + 250ms;
				}
				const bool overlayEnabled = enabled.load(std::memory_order_acquire);
				if (overlayEnabled != windowShown)
				{
					ShowWindow(hwnd.load(), overlayEnabled ? SW_SHOWNOACTIVATE : SW_HIDE);
					windowShown = overlayEnabled;
					if (!overlayEnabled)
					{
						frameSlotReady = true;
						hasPreviousSuccessfulFrame = false;
						framesInWindow = 0;
						peakRenderInWindow = 0.0f;
						peakFrameGapInWindow = 0.0f;
						renderFps.store(0.0f, std::memory_order_relaxed);
						renderMilliseconds.store(0.0f, std::memory_order_relaxed);
						renderPeakMilliseconds.store(0.0f, std::memory_order_relaxed);
						frameGapMilliseconds.store(0.0f, std::memory_order_relaxed);
						frameGapPeakMilliseconds.store(0.0f, std::memory_order_relaxed);
					}
					else
					{
						lastFrameStartedAt = std::chrono::steady_clock::now() - 1s;
						fpsWindowStart = std::chrono::steady_clock::now();
					}
				}
				if (overlayEnabled)
				{
					const std::uint64_t revision = dataVersion.load(std::memory_order_acquire);
					const bool bootstrapFrame = forcePresent.load(std::memory_order_acquire);
					if (revision != renderedVersion && (bootstrapFrame || !frameLatencyWaitable || frameSlotReady))
					{
						int fpsLimit = 0;
						{ std::lock_guard lock(dataMutex); fpsLimit = settings.fpsLimit; }
						// The DXGI waitable object already paces exactly at the compositor rate.
						// A second 144 Hz clock drifts by fractions of a millisecond and skips an
						// entire refresh. Keep the software limiter only for a genuinely lower cap.
						const bool softwareLimited = fpsLimit > 0 &&
							(displayRefreshHz <= 0 || fpsLimit + 1 < displayRefreshHz);
						const auto minimumFrameTime = softwareLimited ?
							std::chrono::microseconds(1000000 / std::max(1, fpsLimit)) : 0us;
						const auto pacingNow = std::chrono::steady_clock::now();
						if (bootstrapFrame || !softwareLimited || pacingNow - lastFrameStartedAt >= minimumFrameTime)
						{
							const auto renderStart = std::chrono::steady_clock::now();
							const HRESULT presentResult = Render();
							const auto renderEnd = std::chrono::steady_clock::now();
							const float renderMs = std::chrono::duration<float, std::milli>(renderEnd - renderStart).count();
							renderMilliseconds.store(renderMs, std::memory_order_relaxed);
							if (SUCCEEDED(presentResult))
							{
								forcePresent.store(false, std::memory_order_release);
								if (frameLatencyWaitable) frameSlotReady = false;
								peakRenderInWindow = std::max(peakRenderInWindow, renderMs);
								if (hasPreviousSuccessfulFrame && !bootstrapFrame)
								{
									const float gapMs = std::chrono::duration<float, std::milli>(renderStart - previousSuccessfulFrameStart).count();
									frameGapMilliseconds.store(gapMs, std::memory_order_relaxed);
									peakFrameGapInWindow = std::max(peakFrameGapInWindow, gapMs);
								}
								else frameGapMilliseconds.store(0.0f, std::memory_order_relaxed);
								hasPreviousSuccessfulFrame = true;
								previousSuccessfulFrameStart = renderStart;
								// Frame limiting is start-to-start. Measuring from renderEnd adds
								// GPU time to the requested interval and skips compositor slots.
								lastFrameStartedAt = renderStart;
								renderedVersion = revision;
								++framesInWindow;
								const float elapsed = std::chrono::duration<float>(renderEnd - fpsWindowStart).count();
								if (elapsed >= 0.5f)
								{
									renderFps.store(framesInWindow / elapsed, std::memory_order_relaxed);
									renderPeakMilliseconds.store(peakRenderInWindow, std::memory_order_relaxed);
									frameGapPeakMilliseconds.store(peakFrameGapInWindow, std::memory_order_relaxed);
									framesInWindow = 0;
									peakRenderInWindow = 0.0f;
									peakFrameGapInWindow = 0.0f;
									fpsWindowStart = renderEnd;
								}
							}
							else
							{
								// Do not leave a transparent window alive with a dead D2D target.
								// Device loss is recoverable and otherwise looks exactly like
								// "notifications work but ESP/radar disappeared" to the player.
								if (RecreateGraphics())
								{
									renderedVersion = 0;
									frameSlotReady = true;
									forcePresent.store(true, std::memory_order_release);
								}
								else
								{
									std::this_thread::sleep_for(50ms);
								}
							}
						}
					}
                }
				// Consume each compositor permit exactly once, remember it until Present,
				// and never discard the result of a second wait at the end of the loop.
				if (frameLatencyWaitable && overlayEnabled && !frameSlotReady)
				{
					if (WaitForSingleObjectEx(frameLatencyWaitable, 2, FALSE) == WAIT_OBJECT_0)
						frameSlotReady = true;
				}
				else
				{
					MsgWaitForMultipleObjectsEx(0, nullptr, 1, QS_ALLINPUT, MWMO_INPUTAVAILABLE);
				}
			}
			targetBitmap.Reset();
			d2dContext.Reset();
			frameLatencyWaitable = nullptr;
			latencySwapChain.Reset();
			swapChain.Reset();
            HWND window = hwnd.exchange(nullptr);
            if (window && IsWindow(window)) DestroyWindow(window);
        }
    };
}

ARK_API void* __stdcall ArkOverlay_Create(std::uint32_t gameProcessId)
{
    if (!gameProcessId) return nullptr;
    auto* state = new (std::nothrow) OverlayState(gameProcessId);
    if (!state) return nullptr;
    if (!state->Start())
    {
        state->Stop();
        delete state;
        return nullptr;
    }
    return state;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateActors(void* handle, const ArkNativeActor* actors,
    std::int32_t count, const ArkNativeSettings* settings)
{
    if (!handle || !settings || count < 0 || count > 10000 || (count > 0 && !actors)) return 0;
    static_cast<OverlayState*>(handle)->UpdateActors(actors, count, *settings);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateTransforms(void* handle, const ArkNativeTransform* transforms,
	std::int32_t count)
{
	if (!handle || count < 0 || count > 10000 || (count > 0 && !transforms)) return 0;
	static_cast<OverlayState*>(handle)->UpdateTransforms(transforms, count);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateMotion(void* handle, const ArkNativeTransform* transforms,
	std::int32_t count, const ArkNativeCamera* camera)
{
	if (!handle || !camera || count < 0 || count > 10000 || (count > 0 && !transforms)) return 0;
	static_cast<OverlayState*>(handle)->UpdateMotion(transforms, count, *camera);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateMotionFrame(void* handle, const ArkNativeMotion* motions,
	std::int32_t count, const ArkNativeCamera* camera)
{
	if (!handle || !camera || count < 0 || count > 10000 || (count > 0 && !motions)) return 0;
	static_cast<OverlayState*>(handle)->UpdateMotionFrame(motions, count, *camera);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateSkeletons(void* handle, const ArkNativeTransform* transforms,
	std::int32_t count)
{
	if (!handle || count < 0 || count > 10000 || (count > 0 && !transforms)) return 0;
	static_cast<OverlayState*>(handle)->UpdateSkeletons(transforms, count);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateStructurePoints(void* handle, const ArkNativeStructurePoint* points,
	std::int32_t count)
{
	if (!handle || count < 0 || count > 10000 || (count > 0 && !points)) return 0;
	static_cast<OverlayState*>(handle)->UpdateStructurePoints(points, count);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateCamera(void* handle, const ArkNativeCamera* camera)
{
    if (!handle || !camera) return 0;
    static_cast<OverlayState*>(handle)->UpdateCamera(*camera);
    return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_UpdateNotifications(void* handle, const ArkNativeNotification* notifications,
	std::int32_t count, std::int32_t corner)
{
	if (!handle || count < 0 || count > 64 || (count > 0 && !notifications) || corner < 0 || corner > 3) return 0;
	static_cast<OverlayState*>(handle)->UpdateNotifications(notifications, count, corner);
	return 1;
}

ARK_API void __stdcall ArkOverlay_SetEnabled(void* handle, std::int32_t enabled)
{
    if (handle) static_cast<OverlayState*>(handle)->SetEnabled(enabled != 0);
}

ARK_API std::int32_t __stdcall ArkOverlay_GetStats(void* handle, float* renderMilliseconds, float* framesPerSecond)
{
	if (!handle || !renderMilliseconds || !framesPerSecond) return 0;
	static_cast<OverlayState*>(handle)->GetStats(*renderMilliseconds, *framesPerSecond);
	return 1;
}

ARK_API std::int32_t __stdcall ArkOverlay_GetStatsEx(void* handle, float* renderMilliseconds,
	float* peakRenderMilliseconds, float* frameGapMilliseconds, float* peakFrameGapMilliseconds, float* framesPerSecond)
{
	if (!handle || !renderMilliseconds || !peakRenderMilliseconds || !frameGapMilliseconds ||
		!peakFrameGapMilliseconds || !framesPerSecond) return 0;
	static_cast<OverlayState*>(handle)->GetStatsEx(*renderMilliseconds, *peakRenderMilliseconds,
		*frameGapMilliseconds, *peakFrameGapMilliseconds, *framesPerSecond);
	return 1;
}

ARK_API void __stdcall ArkOverlay_Destroy(void* handle)
{
    if (!handle) return;
    auto* state = static_cast<OverlayState*>(handle);
    state->Stop();
    delete state;
}

ARK_API std::int32_t __stdcall ArkOverlay_SelfTest()
{
    ArkNativeCamera camera{};
    camera.hasCamera = 1;
    camera.fov = 90.0f;
    ArkNativeActor actor{};
    actor.x = 1000.0f;
    D2D1_POINT_2F center{};
    if (!WorldToScreen(actor, 0.0f, camera, 1000, 800, center)) return 0;
    if (std::abs(center.x - 500.0f) > 0.01f || std::abs(center.y - 400.0f) > 0.01f) return 0;
    actor.y = 100.0f;
    if (!WorldToScreen(actor, 0.0f, camera, 1000, 800, center) || center.x <= 500.0f) return 0;
    return 1;
}
