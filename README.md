# Dynamic Eye Highlight Deformation via Soft-body Simulation in 3D Anime Character Rendering

*Atsuki Haruyama and Yuki Morimoto, Kyushu University*

[ [Paper (SIGGRAPH Asia 2025 Posters)](https://dlnext.acm.org/doi/10.1145/3757374.3771483) ], [ [BibTeX](#citation) ], [ [Source Code](https://github.com/atsuharu-cgresearch/PBHDemo) ]

## Short Abstract
本研究は、弾性体シミュレーションを用いてアニメ調3Dキャラクターの目のハイライトを動的に生成する手法を提案します。反射マッピングの計算過程に、Position Based Dynamics [Müller et al. 2007]のフレームワークを導入することで、手描きアニメ特有の表現と物理的に自然な挙動を、すべてシミュレーションの制約条件として解いて合成します。

We propose a method for generating specular highlight shapes in anime-style 3D character eyes using soft body simulation. Our approach incorporates position-based dynamics (PBD) [Müller et al. 2007] into conventional reflection mapping, enabling a unified framework in which plausible reflections and the stylized expressions we aim to achieve are blended by solving them simultaneously as PBD constraints.

## WebGPU Demo
[![thumbnail](thumbnail.gif)](https://atsuharu-cgresearch.github.io/PBHDemo-WebGPUBuild/)
[![WebGPU Demo](https://img.shields.io/badge/▶_WebGPU_Demo-Play_Now-4CAF50?style=for-the-badge)](https://atsuharu-cgresearch.github.io/PBHDemo-WebGPUBuild/)
[![Unity](https://img.shields.io/badge/Unity-6000.0.49f1-000000?style=flat-square&logo=unity)](https://unity.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square)](./LICENSE)

GIFをクリックするとデモを実行できます。<br>
推奨環境　PC版 Google Chrome・Microsoft Edge（113以降）<br><br>
動作しない場合は、アドレスバーに `chrome://gpu/`（Edgeの場合は `edge://gpu/`）と入力し、
WebGL と WebGPU の項目が `Hardware accelerated` になっているかご確認ください。

## About
本リポジトリは、上記研究手法のUnity実装です。Compute Shaderを用いてGPGPU上でPBDシミュレーションを実装しており、WebGPU環境（ブラウザ上）でリアルタイムに動作します。

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

## Credits
Character Model: RadDollV3 by @三丁目の魔界 ([BOOTH](https://booth.pm/ja/items/3741802))