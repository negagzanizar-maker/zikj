#!/usr/bin/env python3
"""
ZIKJ Local Tracking Simulator
Simulates kart positions without real cameras for laptop testing.
Sends realistic UDP packets to Unity on same machine.

Usage:
    python tracking_simulator.py --karts 6 --fps 60
    
Then in Unity, open any game scene and press T to see incoming packets.
"""

import argparse
import json
import math
import socket
import sys
import time
from dataclasses import dataclass, asdict


@dataclass
class KartState:
    """Simulated kart state."""
    kart_id: int
    x: float
    y: float
    heading: float
    speed: float
    t: float


class TrackingSimulator:
    """Simulate kart tracking data without real cameras."""

    def __init__(self, kart_count: int, target_fps: int):
        """Initialize simulator."""
        self.kart_count = kart_count
        self.target_fps = target_fps
        self.frame_time = 1.0 / target_fps

        # UDP socket
        self.udp_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.target_addr = ("127.0.0.1", 5555)

        # Kart positions (circular track simulation)
        self.karts = {}
        self.initialize_karts()

        # Metrics
        self.packets_sent = 0
        self.start_time = time.time()

    def initialize_karts(self):
        """Set up initial kart positions around track."""
        # Stadium track: top straight y=200, bottom straight y=520
        # Left side x=280, right side x=1000
        track_positions = [
            (400, 200),   # Top straight, left
            (650, 200),   # Top straight, center
            (900, 200),   # Top straight, right
            (900, 520),   # Bottom straight, right
            (650, 520),   # Bottom straight, center
            (400, 520),   # Bottom straight, left
        ]

        for i in range(self.kart_count):
            pos = track_positions[i % len(track_positions)]
            self.karts[i] = {
                "x": pos[0],
                "y": pos[1],
                "heading": 0.0,
                "speed": 10.0 + i * 2,  # Varying speeds
                "direction": 1 if i % 2 == 0 else -1,  # CW or CCW
            }

    def update_positions(self, dt: float):
        """Simulate kart movement around track."""
        for kart_id, kart in self.karts.items():
            speed = kart["speed"]
            direction = kart["direction"]

            # Simple track following (serpentine for variety)
            if kart["y"] < 250:  # Top straight
                kart["x"] += speed * direction * dt * 50  # 50 scale factor
                kart["heading"] = 0.0 if direction > 0 else math.pi
                if kart["x"] > 950:
                    kart["direction"] = -1
                    kart["y"] = 250
                elif kart["x"] < 300:
                    kart["direction"] = 1
                    kart["y"] = 250
            elif kart["y"] > 470:  # Bottom straight
                kart["x"] += speed * direction * dt * 50
                kart["heading"] = 0.0 if direction > 0 else math.pi
                if kart["x"] > 950:
                    kart["direction"] = -1
                    kart["y"] = 470
                elif kart["x"] < 300:
                    kart["direction"] = 1
                    kart["y"] = 470
            else:  # Vertical sections
                kart["y"] += speed * direction * dt * 50
                kart["heading"] = math.pi / 2 if direction > 0 else -math.pi / 2

            # Vary speed slightly (simulate acceleration/braking)
            kart["speed"] += (math.sin(time.time() + kart_id) * 0.5)
            kart["speed"] = max(5.0, min(30.0, kart["speed"]))

    def send_kart_state(self, kart_id: int):
        """Send single kart state to Unity."""
        kart = self.karts[kart_id]
        state = KartState(
            kart_id=kart_id,
            x=kart["x"],
            y=kart["y"],
            heading=kart["heading"],
            speed=kart["speed"],
            t=time.time(),
        )

        try:
            payload = json.dumps(asdict(state))
            self.udp_socket.sendto(payload.encode(), self.target_addr)
            self.packets_sent += 1
        except Exception as e:
            print(f"[ERROR] Failed to send: {e}")

    def run(self):
        """Main simulation loop."""
        print(f"[SIM] Starting tracking simulator...")
        print(f"[SIM] Simulating {self.kart_count} karts at {self.target_fps} Hz")
        print(f"[SIM] Sending to {self.target_addr[0]}:{self.target_addr[1]}")
        print(f"[SIM] Open Unity scene and press T to see packets\n")

        last_frame_time = time.time()
        stats_time = time.time()

        try:
            while True:
                now = time.time()
                elapsed = now - last_frame_time

                if elapsed >= self.frame_time:
                    # Update positions
                    dt = elapsed
                    self.update_positions(dt)

                    # Send all kart states
                    for kart_id in range(self.kart_count):
                        self.send_kart_state(kart_id)

                    last_frame_time = now

                    # Log stats every 5 seconds
                    if now - stats_time > 5.0:
                        elapsed_total = now - self.start_time
                        rate = self.packets_sent / elapsed_total
                        print(
                            f"[SIM] Packets sent: {self.packets_sent}, "
                            f"Rate: {rate:.1f} Hz, "
                            f"Avg per frame: {self.packets_sent / (elapsed_total / self.frame_time):.1f}"
                        )
                        stats_time = now

                # Sleep a bit to avoid busy-waiting
                time.sleep(0.001)

        except KeyboardInterrupt:
            print("\n[SIM] Stopping simulator...")
            self.shutdown()

    def shutdown(self):
        """Clean up."""
        self.udp_socket.close()
        print(f"[SIM] Sent {self.packets_sent} packets total")
        print("[SIM] Simulator stopped.")


def main():
    parser = argparse.ArgumentParser(description="ZIKJ Tracking Simulator")
    parser.add_argument("--karts", type=int, default=6, help="Number of karts to simulate")
    parser.add_argument("--fps", type=int, default=60, help="Packet rate (Hz)")
    args = parser.parse_args()

    simulator = TrackingSimulator(args.karts, args.fps)
    simulator.run()


if __name__ == "__main__":
    main()
