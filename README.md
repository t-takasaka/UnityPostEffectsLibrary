# Unity Post Effects Library

Unity用のポストエフェクトライブラリです。

キャラクターコンテンツ向けのエフェクトを中心に実装していく予定です。

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr01.png" width="400"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr02.png" width="400"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr03.png" width="400"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr04.png" width="400">

## エフェクト

下記のエフェクトを実装済みです。

Main Camera に PostEffects.cs をアタッチし、インスペクタからパラメータを調整して使用してください。

- Stroke Based Rendering

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/sbr_earth.png" width="100">

- Watercolor Rendering

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/wcr_earth.png" width="100">

- Anisotropic Kuwahara Filter

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/akf_earth.png" width="100">

- Symmetric Nearest Neighbor

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/snn_earth.png" width="100">

- Bilateral Filter

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/bf_earth.png" width="100">

- (Source Image)

<img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_lenna.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_mandrill.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_sailboat.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_parrots.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_milkdrop.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_pepper.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_airplane.png" width="100"> <img src="https://raw.githubusercontent.com/t-takasaka/UnityPostEffectsLibrary/master/Media/src_earth.png" width="100">

## 参考

- [Structure Adaptive Stylization of Images and Video](https://publishup.uni-potsdam.de/opus4-ubp/frontdoor/deliver/index/docId/6174/file/kyprianidis_diss.pdf)

- [ShaderX7](https://www.amazon.com/dp/1584505982)

- [GPU Pro 4](https://www.amazon.co.jp/dp/B00CEN4RNW/)

- [Towards Photo Watercolorization with Artistic Verisimilitude](http://www.cs.columbia.edu/~fyun/watercolor/watercolor_pp.pdf)

## ライセンス

MITライセンスで提供しています。

以下のモデルやライブラリを使用しています。依存関係はありませんのでアプリに適したデータに置き換えて利用することが可能です。下記のデータをそのまま使用する場合、各ライセンスは配布元に従ってください。

- モデルは VRoid を使用しています。

https://vroid.pixiv.net/

- モーションは mocapdata.com, Eyes, JAPAN Co. Ltd. により「クリエイティブ・コモンズ表示 2.1 日本ライセンス」の下でライセンスされている BVH を変換・使用しています。

http://mocapdata.com/

- デモはユニティちゃんライブステージ！ -Candy Rock Star- を使用しています。

(c) Unity Technologies Japan/UCL

http://unity-chan.com/

- VRM の読み込みは UniVRM を使用しています。

https://github.com/vrm-c/UniVRM



