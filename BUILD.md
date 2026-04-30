# Build

## 构建与打包

1. Copy `Local.props.example` to `Local.props`.
2. 在 `Local.props` 中填写本机 DSP Managed 路径和依赖 DLL 路径。
3. 首次构建前运行 `scripts\setup.cmd`。
4. 运行 `scripts\package.cmd` 生成 r2modman/Thunderstore 格式的发布包，发布包会生成到 `dist/`。
