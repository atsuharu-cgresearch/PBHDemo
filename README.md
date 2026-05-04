# Dynamic Eye Highlight Deformation via Soft-body Simulation in 3D Anime Character Rendering

*[Atsuki Haruyama](https://github.com/atsuharu-cgresearch) and Yuki Morimoto, Kyushu University*

[ [Paper (SIGGRAPH Asia 2025 Posters)](https://dlnext.acm.org/doi/10.1145/3757374.3771483) ], [ [BibTeX](#citation) ], [ [Source Code](https://github.com/atsuharu-cgresearch/PBHDemo) ]

## Short Abstract
We propose a method for generating specular highlight shapes in anime-style 3D character eyes using soft body simulation. Our approach incorporates position-based dynamics (PBD) [Müller et al. 2007] into conventional reflection mapping, enabling a unified framework in which plausible reflections and the stylized expressions we aim to achieve are blended by solving them simultaneously as PBD constraints.

## WebGPU Demo

<a href="https://atsuharu-cgresearch.github.io/PBHDemo-WebGPUBuild/">
  <img src="https://placehold.jp/720x360.png" alt="Demo Preview" width="100%">
</a>

[![WebGPU Demo](https://img.shields.io/badge/▶_WebGPU_Demo-Play_Now-4CAF50?style=for-the-badge)](https://atsuharu-cgresearch.github.io/PBHDemo-WebGPUBuild/)
[![Unity](https://img.shields.io/badge/Unity-6000.0.49f1-000000?style=flat-square&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](./LICENSE)

<br>

## About
本リポジトリは、弾性体シミュレーション（Position Based Dynamics）を用いて、アニメ風3Dキャラクターモデルの目のハイライトを動的に生成する手法のUnity実装です。

Compute Shaderを用いたGPGPUによる並列化を行い、WebGPU環境（ブラウザ上）でもリアルタイムかつ高速に動作するよう最適化されています。

## Requirements
本プロジェクトをローカルで実行・ビルドするためのシステム要件です。

- **OS:** Windows 10/11 または macOS 12 以降
- **Unity Version:** Unity 6 (6000.0.49f1) 以降
- **Required Modules:** WebGL Build Support、WebGPU (Experimental)
- **Target Platform:** WebGPU / PC Standalone
- **GPU:** WebGPU対応GPU（DirectX 12 / Vulkan / Metal）
- **Browser (WebGPU build用):** Google Chrome または Microsoft Edge（バージョン 113 以降）

<br>

## Getting Started
Unityエディタで本プロジェクトを開き、実行するための手順です。
```bash
# リポジトリのクローン
git clone https://github.com/atsuharu-cgresearch/PBHDemo.git
```

1. **Unity Hub** を起動し、`Open` から clone したフォルダを選択する
2. Unity 6 (6000.0.49f1) 以降でプロジェクトを開く
3. `Assets/Scenes/` から `Demo.scene`を開く
4. エディタ上部の **Play ボタン** を押して動作確認する

## Citation

If you find this project useful for your research or work, please use the following BibTeX entry:

```bibtex
@inbook{10.1145/3757374.3771483,
  author = {Haruyama, Atsuki and Morimoto, Yuki},
  title = {Dynamic Eye Highlight Deformation via Soft-body Simulation in 3D Anime Character Rendering},
  year = {2025},
  isbn = {9798400721342},
  publisher = {Association for Computing Machinery},
  address = {New York, NY, USA},
  url = {[https://doi.org/10.1145/3757374.3771483](https://doi.org/10.1145/3757374.3771483)},
  booktitle = {Proceedings of the SIGGRAPH Asia 2025 Posters},
  articleno = {64},
  numpages = {2}
}