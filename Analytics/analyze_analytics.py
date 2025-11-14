import argparse
from pathlib import Path

import numpy as np
import pandas as pd

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt


BASE_MOVEMENT_SPEED = 6.0  

COLORS = {
    "blue": "#0072B2",
    "orange": "#E69F00",
    "green": "#009E73",
    "red": "#D55E00",
    "purple": "#CC79A7",
    "gray": "#999999",
    "yellow": "#F0E442",
}

plt.rcParams["axes.prop_cycle"] = matplotlib.cycler(color=[
    COLORS["blue"], COLORS["orange"], COLORS["green"],
    COLORS["red"], COLORS["purple"], COLORS["yellow"]
])
plt.rcParams["figure.dpi"] = 120
plt.rcParams["savefig.dpi"] = 200
plt.rcParams["axes.grid"] = True
plt.rcParams["grid.alpha"] = 0.25


# -------------------- Load & Clean --------------------

def load_and_clean(csv_path: Path) -> pd.DataFrame:
    """Load the raw CSV and compute core derived columns for metrics."""
    df = pd.read_csv(csv_path)

    # Normalize column names
    df.columns = [c.strip() for c in df.columns]

    required = [
        "session_id",
        "level_id",
        "attempt_number_in_session",
        "level_outcome",
        "level_duration_ms",
        "total_kill_attempts",
        "correct_kills",
        "wrong_kills",
        "avg_speed",
        "mean_reaction_latency_ms",
    ]
    missing = [c for c in required if c not in df.columns]
    if missing:
        raise ValueError(f"Missing required columns in CSV: {missing}")

    nd = pd.DataFrame()
    nd["session_id"] = df["session_id"].astype(str)
    nd["level_id"] = df["level_id"].astype(str)
    nd["attempt_number_in_session"] = pd.to_numeric(
        df["attempt_number_in_session"], errors="coerce"
    )

    nd["level_outcome"] = df["level_outcome"].astype(str).str.lower()

    nd["level_duration_ms"] = pd.to_numeric(
        df["level_duration_ms"], errors="coerce"
    )
    nd["level_duration_s"] = nd["level_duration_ms"] / 1000.0

    nd["total_kill_attempts"] = pd.to_numeric(
        df["total_kill_attempts"], errors="coerce"
    )
    nd["correct_kills"] = pd.to_numeric(
        df["correct_kills"], errors="coerce"
    )
    nd["wrong_kills"] = pd.to_numeric(
        df["wrong_kills"], errors="coerce"
    )

    nd["avg_speed"] = pd.to_numeric(df["avg_speed"], errors="coerce")

    nd["mean_reaction_latency_ms"] = pd.to_numeric(
        df["mean_reaction_latency_ms"], errors="coerce"
    )
    nd["mean_reaction_latency_s"] = nd["mean_reaction_latency_ms"] / 1000.0

    # Optional timestamp: try to parse if present (common names)
    ts_col = None
    for candidate in ["timestamp", "Timestamp", "submitted_at", "submission_time"]:
        if candidate in df.columns:
            ts_col = candidate
            break

    if ts_col is not None:
        nd["timestamp"] = pd.to_datetime(df[ts_col], errors="coerce")
    else:
        # Fallback synthetic time ordering: by session and attempt
        nd["timestamp"] = pd.NaT

    # --------- Derived Metrics ---------

    # Metric 1: Adaptive Speed Ratio (ASR)
    nd["ASR"] = np.where(
        BASE_MOVEMENT_SPEED > 0,
        nd["avg_speed"] / BASE_MOVEMENT_SPEED,
        np.nan,
    )

    # Metric 2: Reaction Latency (RL) – seconds
    nd["RL_s"] = nd["mean_reaction_latency_s"]
    nd["RL_s_clamped"] = nd["RL_s"].clip(upper=30)

    # Metric 4: Decision Accuracy Ratio (DAR)
    nd["DAR"] = np.where(
        nd["total_kill_attempts"] > 0,
        nd["correct_kills"] / nd["total_kill_attempts"],
        np.nan,
    )

    # Replace inf with NaN early
    nd = nd.replace([np.inf, -np.inf], np.nan)

    # Drop rows that are completely empty on key fields
    nd = nd.dropna(
        subset=["avg_speed", "level_duration_s", "total_kill_attempts", "correct_kills"],
        how="all",
    )

    return nd


# -------------------- Plot Helpers --------------------

def placeholder_plot(title, path: Path):
    plt.figure()
    plt.text(
        0.5,
        0.5,
        "No Data Available",
        ha="center",
        va="center",
        fontsize=14,
        color=COLORS["red"],
    )
    plt.title(title)
    plt.axis("off")
    plt.tight_layout()
    plt.savefig(path)
    plt.close()


# -------------------- Metric Visualizations (One-per-metric) --------------------

def plot_asr_density(df: pd.DataFrame, out_dir: Path):
    """
    ASR: Density curve (KDE plot) per level.
    Smooth, visually appealing distribution visualization.
    """
    path = out_dir / "metric_ASR_density.png"
    sub = df[["ASR", "level_id"]].dropna()

    if sub.empty:
        placeholder_plot("Adaptive Speed Ratio (Density Plot)", path)
        return

    levels = sorted(sub["level_id"].unique())
    plt.figure(figsize=(8, 4))

    for i, lvl in enumerate(levels):
        data = sub.loc[sub["level_id"] == lvl, "ASR"].dropna()
        if len(data) == 0:
            continue

        # Create KDE (density curve)
        try:
            from scipy.stats import gaussian_kde
            kde = gaussian_kde(data)
            xs = np.linspace(data.min(), data.max(), 300)
            ys = kde(xs)

            color = plt.rcParams["axes.prop_cycle"].by_key()["color"][i % 10]
            plt.plot(xs, ys, label=f"Level {lvl}", linewidth=2, color=color)
            plt.fill_between(xs, ys, alpha=0.15, color=color)

        except Exception:
            # Fallback simple hist (rare case)
            plt.hist(data, bins=20, alpha=0.5, density=True, label=f"Level {lvl}")

    plt.xlabel("Adaptive Speed Ratio (ASR)")
    plt.ylabel("Density")
    plt.title("ASR Distribution (Density Curve per Level)")
    plt.legend()
    plt.tight_layout(rect=[0.05, 0.05, 0.95, 0.95])
    plt.savefig(path)
    plt.close()




def plot_rl_histogram(df: pd.DataFrame, out_dir: Path):
    """
    RL: histogram of reaction latency with mean/median lines.
    """
    path = out_dir / "metric_RL_hist.png"
    data = df["RL_s_clamped"].dropna()
    if data.empty:
        placeholder_plot("Reaction Latency (Histogram)", path)
        return

    mean_val = data.mean()
    median_val = data.median()

    plt.figure(figsize=(7, 4))
    plt.hist(data, bins=20, alpha=0.7, edgecolor="black")
    plt.xlabel("Reaction Latency (s, clamped at 30)")
    plt.ylabel("Count")
    plt.title("Distribution of Reaction Latency")

    # Reference lines
    plt.axvline(mean_val, color=COLORS["red"], linestyle="--", linewidth=1.2, label=f"Mean = {mean_val:.2f}s")
    plt.axvline(median_val, color=COLORS["green"], linestyle="-.", linewidth=1.2, label=f"Median = {median_val:.2f}s")

    plt.legend()
    plt.tight_layout(rect=[0.05, 0.05, 0.95, 0.95])
    plt.savefig(path)
    plt.close()


# -------------------- Retry Motivation Index (RMI) --------------------
# Implemented from *level_attempt* rows only.
#
# For each (session_id, level_id):
#  - If there is at least one 'fail' AND
#    there exists an attempt_number_in_session greater than the first fail attempt
#    on that same level in that same session → that pair counts as "retried after failure".
#
# Then for each session_id, aggregate across levels:
#   RMI_session = (# of levels where player retried after a fail)
#                 / (# of levels where player had at least one fail) * 100

def compute_rmi(df: pd.DataFrame) -> pd.DataFrame:
    grouped = df.sort_values("attempt_number_in_session").groupby(
        ["session_id", "level_id"], dropna=False
    )

    records = []
    for (session_id, level_id), group in grouped:
        group = group.copy()
        group["level_outcome"] = group["level_outcome"].astype(str).str.strip().str.lower()

        fail_mask = group["level_outcome"].eq("fail")
        had_fail = fail_mask.any()
        retried = False

        if had_fail:
            first_fail_attempt = group.loc[fail_mask, "attempt_number_in_session"].min()
            retried = group["attempt_number_in_session"].gt(first_fail_attempt).any()

            if not retried and "retry_after_failure" in group:
                retried = (
                    group.get("retry_after_failure", pd.Series(index=group.index))
                    .fillna(0)
                    .astype(float)
                    .gt(0)
                    .any()
                )

            if not retried and "retry_count_within_5min" in group:
                retried = (
                    group.get("retry_count_within_5min", pd.Series(index=group.index))
                    .fillna(0)
                    .astype(float)
                    .gt(0)
                    .any()
                )

        records.append(
            {
                "session_id": session_id,
                "level_id": level_id,
                "had_fail": had_fail,
                "retried_after_failure": retried,
            }
        )

    per_pair = pd.DataFrame.from_records(records)
    per_level = (
        per_pair.groupby("level_id")
        .agg(fail_sessions=("had_fail", "sum"), retry_sessions=("retried_after_failure", "sum"))
        .reset_index()
        .sort_values("level_id")
    )

    per_level["RMI_pct"] = np.where(
        per_level["fail_sessions"] > 0,
        100.0 * per_level["retry_sessions"] / per_level["fail_sessions"],
        np.nan,
    )

    return per_level


def plot_rmi(per_level_rmi: pd.DataFrame, out_dir: Path) -> None:
    path = out_dir / "metric_RMI_per_level_bar.png"
    if per_level_rmi.empty or per_level_rmi["RMI_pct"].dropna().empty:
        placeholder_plot("Retry Motivation Index (Per Level)", path)
        return

    vals = np.nan_to_num(per_level_rmi["RMI_pct"].to_numpy(dtype=float))
    x = np.arange(len(per_level_rmi))

    plt.figure(figsize=(7, 4))
    bars = plt.bar(x, vals, color=COLORS["green"], alpha=0.85)
    plt.xticks(x, per_level_rmi["level_id"])
    plt.ylabel("RMI (% of failure cases where a retry occurred)")
    plt.title("Retry Motivation Index by Level")

    for rect, val in zip(bars, vals):
        if np.isfinite(val):
            plt.text(
                rect.get_x() + rect.get_width() / 2,
                max(val, 0) + 1.0,
                f"{val:.1f}%",
                ha="center",
                va="bottom",
                fontsize=9,
            )

    plt.tight_layout(rect=[0, 0.02, 1, 0.98])
    plt.savefig(path)
    plt.close()


def plot_dar_per_level_bar(df: pd.DataFrame, out_dir: Path):
    """
    DAR: bar chart by level on a 0–1 axis.
    """
    path = out_dir / "metric_DAR_per_level_bar.png"
    if df["DAR"].dropna().empty:
        placeholder_plot("Decision Accuracy Ratio (Per Level)", path)
        return

    per_level = (
        df.groupby("level_id")
        .agg(
            mean_DAR=("DAR", "mean"),
            std_DAR=("DAR", "std"),
            n=("DAR", "count"),
        )
        .reset_index()
        .sort_values("level_id")
    )

    vals = per_level["mean_DAR"].values.astype(float)
    errs = per_level["std_DAR"].fillna(0).values.astype(float)
    x = np.arange(len(per_level))

    vals = np.nan_to_num(vals, nan=0.0, posinf=0.0, neginf=0.0)
    errs = np.nan_to_num(errs, nan=0.0, posinf=0.0, neginf=0.0)

    plt.figure(figsize=(7, 4))
    bars = plt.bar(x, vals, yerr=errs, capsize=4, color=COLORS["orange"], alpha=0.85)
    plt.xticks(x, per_level["level_id"])
    plt.ylabel("Decision Accuracy Ratio (DAR)")
    plt.ylim(0, 1.0)
    plt.title("Decision Accuracy by Level")

    for rect, v in zip(bars, vals):
        if np.isfinite(v):
            plt.text(
                rect.get_x() + rect.get_width() / 2,
                v + 0.02,
                f"{v:.2f}",
                ha="center",
                va="bottom",
                fontsize=9,
            )

    plt.tight_layout(rect=[0.05, 0.05, 0.95, 0.95])
    plt.savefig(path)
    plt.close()


# -------------------- Summaries / Diagnostics --------------------

def export_summary_tables(df: pd.DataFrame, per_session_rmi: pd.DataFrame, out_dir: Path):
    per_level = (
        df.groupby("level_id")
        .agg(
            sessions=("session_id", "nunique"),
            mean_ASR=("ASR", "mean"),
            median_ASR=("ASR", "median"),
            mean_RL_s=("RL_s", "mean"),
            median_RL_s=("RL_s", "median"),
            mean_DAR=("DAR", "mean"),
            median_DAR=("DAR", "median"),
        )
        .reset_index()
        .sort_values("level_id")
    )

    per_level.to_csv(out_dir / "summary_per_level_new_metrics.csv", index=False)

    global_summary = pd.DataFrame(
        [
            {
                "sessions": df["session_id"].nunique(),
                "mean_ASR": df["ASR"].mean(),
                "median_ASR": df["ASR"].median(),
                "mean_RL_s": df["RL_s"].mean(),
                "median_RL_s": df["RL_s"].median(),
                "mean_DAR": df["DAR"].mean(),
                "median_DAR": df["DAR"].median(),
            }
        ]
    )
    global_summary.to_csv(out_dir / "summary_global_new_metrics.csv", index=False)

    per_session_rmi.to_csv(out_dir / "summary_RMI_per_session.csv", index=False)


def write_diagnostics(df: pd.DataFrame, out_dir: Path):
    lines = []
    lines.append(f"Rows after cleaning: {len(df)}")
    lines.append(f"Unique sessions: {df['session_id'].nunique()}")
    levels = ", ".join(sorted(map(str, df["level_id"].unique()))) if len(df) else "(none)"
    lines.append(f"Levels: {levels}")

    if df["ASR"].std(ddof=0) > 0 and df["DAR"].std(ddof=0) > 0:
        corr = np.corrcoef(df["ASR"].fillna(0), df["DAR"].fillna(0))[0, 1]
        lines.append(f"Corr(ASR, DAR): {corr:.3f}")

    if df["RL_s"].std(ddof=0) > 0 and df["DAR"].std(ddof=0) > 0:
        corr = np.corrcoef(df["RL_s"].fillna(0), df["DAR"].fillna(0))[0, 1]
        lines.append(f"Corr(RL_s, DAR): {corr:.3f}")

    (out_dir / "diagnostics_new_metrics.txt").write_text(
        "\n".join(lines), encoding="utf-8"
    )


# -------------------- Main --------------------

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--csv", required=True, help="Path to metrics CSV")
    ap.add_argument("--out", default="out_charts_new", help="Output directory")
    args = ap.parse_args()

    out_dir = Path(args.out)
    out_dir.mkdir(parents=True, exist_ok=True)

    df = load_and_clean(Path(args.csv))
    df.to_csv(out_dir / "cleaned_metrics_new.csv", index=False)

    # Metric plots (one per metric)
    plot_asr_density(df, out_dir)
    plot_rl_histogram(df, out_dir)
    plot_dar_per_level_bar(df, out_dir)

    # RMI per session and leaderboard
    rmi_df = compute_rmi(df)
    rmi_df.to_csv(out_dir / "rmi_per_level.csv", index=False)
    plot_rmi(rmi_df, out_dir)

    # Summaries / diagnostics
    export_summary_tables(df, rmi_df, out_dir)
    write_diagnostics(df, out_dir)

    print("✅ New metric charts and summaries saved in:", out_dir.resolve())


if __name__ == "__main__":
    main()
