# Tower Defense Project Structure Overview
Updated: 2026-04-07
Audience: 缁欓」鐩淮鎶よ€呯湅鐨勭粨鏋勮鏄庢枃妗ｃ€?Goal: 鐢ㄦ洿濂借鐨勬柟寮忚娓呮褰撳墠椤圭洰鐢卞摢浜涘満鏅€佽剼鏈€乁I銆佹枃妗ｅ拰宸ュ叿缁勬垚锛屼互鍙婂畠浠€庝箞鍗忎綔銆?
## 1. 涓€鍙ヨ瘽鐞嗚В杩欎釜椤圭洰
杩欐槸涓€涓?Unity 2022.3 鐨?2D 濉旈槻鍘熷瀷椤圭洰銆?褰撳墠鏍稿績鐗硅壊鏄細
- 鐢甸噺缁忔祹
- 鑷敱鎷栨嫿閮ㄧ讲
- 棣栧鍒濆閮ㄧ讲鍖?+ 鍚庣画娌垮缓绛戠綉缁滄墿寮?- 鎷栨嫿鏃舵樉绀虹簿纭悎娉曞尯鍩熻鐩栧眰

褰撳墠鍏ュ彛娴佺▼鏄細
- 鍏堣繘鍏?`MainMenu`
- 鐐瑰嚮寮€濮嬭繘鍏?`SampleScene`

## 2. 椤圭洰鏈€閲嶈鐨勪袱涓満鏅?### `Assets/Scenes/MainMenu.unity`
杩欐槸涓婚〉闈㈠満鏅€?浣犳墦寮€瀹冩椂锛宍MainMenuController` 浼氬湪缂栬緫鍣ㄩ噷鎶婇粯璁?UI 楠ㄦ灦琛ラ綈鍒板満鏅腑锛屾墍浠ヤ綘鍙互鐩存帴鍦?Scene 鍜?Inspector 閲岃皟鏍囬銆佹寜閽€佽鏄庢枃瀛椼€佸竷灞€绛夊唴瀹广€傚綋鍓嶈彍鍗?Canvas 涔熷凡鍒囧埌 `Screen Space Camera`锛屽湪 Scene 閲岃瀵熷拰鎷栧姩浼氭洿鐩磋銆?
缁х画杩欒疆鍚庯紝`MainMenuController` 鍦ㄢ€滈〉闈㈠凡鎴愬瀷鈥濋樁娈典篃鏀规垚浜嗘樉寮忓紩鐢ㄤ紭鍏堬細瀹冧笉鍐嶆寜瀵硅薄鍚嶅洖濉富鑿滃崟瀛愯妭鐐癸紝鍙湪缂哄紩鐢ㄦ椂杈撳嚭鍛婅锛涘璞″悕鐜板湪涓昏鍙湇鍔′簬棣栨鐢熸垚榛樿楠ㄦ灦銆?
瀹冪殑涓昏浣滅敤鍙湁涓€涓細
- 浣滀负娓告垙鍏ュ彛锛岀偣鍑诲紑濮嬭繘鍏?`SampleScene`

### `Assets/Scenes/SampleScene.unity`
杩欐槸褰撳墠濉旈槻鐜╂硶鐪熸杩愯鐨勫満鏅€?杩欓噷闈㈠寘鍚細
- 鎴樺満
- 鏁屼汉璺緞
- 寤洪€犲尯
- 绂佸缓鍖?- 杩愯鏃舵牴鑺傜偣
- HUDCanvas
- 鍙充晶閮ㄧ讲鍖?- 鐜╂硶鎬绘帶瀵硅薄

濡傛灉浣犳兂鏀圭湡姝ｇ殑濉旈槻鐜╂硶锛屽ぇ澶氭暟鏃跺€欓兘浼氬洖鍒拌繖涓満鏅€?
褰撳墠鍏冲崱鐜板湪杩樹細鍦ㄢ€滃満涓婅繕娌℃湁浠讳綍宸插缓濉斺€濇椂锛屼簬鍦板浘涓婂父椹绘樉绀洪濉旇捣鎵嬪尯鏍囪锛涚涓€搴у鏀句笅鍚庝細鑷姩闅愯棌銆?
缂栬緫鐘舵€佷笅锛宍GameController` 杩樹細閫氳繃 Scene Gizmos 鐩存帴鎶婅繖鍧楄捣鎵嬪尯鐢诲嚭鏉ワ紝鎵€浠ヤ笉杩?Play 涔熻兘鍦?Scene 瑙嗗浘閲岀湅瑙併€?
褰撳墠鐜╂硶 HUD 涓庝富鑿滃崟 UI 瀵硅薄涔熼兘宸茬粡鍒囧埌 Unity 鐨?`UI` Layer锛屾柟渚夸綘鍦?Scene 瑙嗗浘鍙充笂瑙掔敤 `Layers` 杩囨护鍙湅鐣岄潰銆?
## 3. 鏍稿績鑴氭湰鍒嗗伐
### `TowerDefenseGame.cs`
杩欐槸椤圭洰鎬绘帶銆?瀹冭礋璐ｆ暣灞€娓告垙鐨勮鍒欑紪鎺掞細
- 璧勬簮鍜屽熀鍦拌閲?- 褰撳墠閫変腑鐨勫缓绛戠被鍨?- 鐐瑰嚮/鎷栨嫿鏀剧疆
- 鏀剧疆鏄惁鍚堟硶
- 鏀剧疆棰勮濉?- 绮剧‘鍚堟硶鍖鸿鐩栧眰
- 鐪熸寤哄
- 鍒?HUD
- Game Over

鍙互鎶婂畠鐞嗚В鎴愨€滃婕斺€濄€?
褰撳墠瀹冧篃宸茬粡寮€濮嬬涓€闃舵鍘绘帀鈥滄寜瀵硅薄鍚嶆煡鎵惧満鏅璞♀€濈殑杩佺Щ锛氭牳蹇冨満鏅緷璧栫幇宸叉敮鎸?Inspector 鐩存嫋浼樺厛锛宍SampleScene` 閲屽叧閿繍琛屾椂寮曠敤涔熷凡缁忔帴涓娿€?鍑犱箮鎵€鏈変富娴佺▼鏈€鍚庨兘浼氭敹鍙ｅ埌杩欓噷銆?
褰撳墠瀹冧篃宸茬粡寮€濮嬩粠鈥滄寜瀵硅薄鍚嶆煡鎵锯€濊縼鍒扳€滄樉寮忓満鏅紩鐢ㄤ紭鍏堚€濈殑妯″紡锛宍SampleScene` 閲岀殑鏍稿績杩愯鏃跺璞″拰涓€鎵逛富 HUD 寮曠敤閮藉凡缁忔帴涓娿€?
缁х画鎺ㄨ繘鍚庯紝`TowerDefenseGame` 鍜?`WaveSpawner` 鐨勪富鍦烘櫙渚濊禆宸茬粡涓嶅啀闈犲悕瀛楁煡鎵炬嫾涓婚摼锛涘綋鍓嶅墿浣欑殑鍚嶅瓧鍏滃簳涓昏闆嗕腑鍦?HUD 琛ㄧ幇灞傘€?
缁х画杩欎竴杞悗锛孒UD 琛ㄧ幇灞備篃鏀规垚浜嗏€滄樉寮忕粦瀹氬紩鐢?+ 缂洪」鍛婅鈥濈殑妯″紡锛屾墍浠ュ綋鍓嶄富鐜╂硶鍦烘櫙鐨勬牳蹇冭繍琛岄摼璺凡缁忓熀鏈笉鍐嶄富鍔ㄤ緷璧栧璞″悕鏌ユ壘銆?
鍚屾椂锛宍TowerDefenseGame` 涔熶笉鍐嶇粰 HUD presenter 浼犱竴鏁村瀵硅薄鍚嶉厤缃紝涓婚摼浠ｇ爜琛ㄨ揪浼氭洿鐩存帴銆?
缁х画杩欒疆鍚庯紝HUD presenter 鑷繁閭ｅ眰鏃у悕瀛楀瓧娈典篃宸茬粡娓呮帀浜嗭紝鎵€浠ヨ繖鏉′富閾剧幇鍦ㄤ粠浠ｇ爜缁撴瀯涓婁篃鏇存帴杩戠湡姝ｇ殑鏄惧紡瑁呴厤銆?
### `TowerCatalog.cs`
杩欐槸寤虹瓚闈欐€佸畾涔変腑蹇冦€?瀹冭礋璐ｅ洖绛斺€滄煇绉嶅缓绛戞槸浠€涔堚€濓紝鑰屼笉鏄€滆繖灞€閲岀幇鍦ㄥ彂鐢熶簡浠€涔堚€濄€?褰撳墠涓昏鎻愪緵锛?- 灞曠ず鍚?- 鎴愭湰
- 鍗犲湴鍗婂緞
- 鎵╁紶鏂规牸澶у皬
- 鍗＄墖鏂囨
- 寮鸿皟鑹?
濡傛灉浣犱互鍚庤鏀光€滃彂鐢垫満鏄笉鏄彨 Relay Generator鈥濃€滅偖濉斿崱鐗囦笂鍐欎粈涔堚€濃€滄煇绉嶅缓绛戦摵璺寖鍥存湁澶氬ぇ鈥濓紝浼樺厛鐪嬭繖閲屻€?
### `TowerDefenseHudPresenter.cs`
杩欐槸 HUD 琛ㄧ幇灞傘€?瀹冧笉鍐冲畾瑙勫垯锛屽彧璐熻矗鎶婅鍒欑粨鏋滄樉绀哄嚭鏉ャ€?褰撳墠瀹冧富瑕佸仛锛?- 鏌ユ壘 HUD 鑺傜偣
- 鏇存柊椤堕儴璧勬簮鍗?- 鏇存柊鍙充晶閮ㄧ讲鍗?- 鏇存柊鎷栨嫿鎻愮ず闈㈡澘
- 杩愯鏃剁編鍖?HUD
- 杩愯鏃舵樉寮忎慨姝ｅ竷灞€锛岄伩鍏嶉噸鍙?
鐜板湪鐜╂硶 HUD 鐨勬帹鑽愮淮鎶ゆ柟寮忔槸锛?
- 甯冨眬銆佷綅缃€佸ぇ灏忋€侀鑹诧細浼樺厛鐩存帴鏀?`SampleScene` 閲岀殑鐪熷疄 UI 瀵硅薄
- 璧勬簮鏁板€笺€佹尝娆°€佺姸鎬佹彁绀猴細浠嶇敱鑴氭湰鍔ㄦ€佸埛鏂?
- 鎸夐挳涓嶅彲鐢ㄦ椂鐨勮繃娓¤壊锛氫紭鍏堝湪 Button 鑷繁鐨?Transition / ColorBlock 閲屾敼

褰撳墠瀹冧篃宸茬粡鏀寔鈥滃閮ㄦ樉寮忔敞鍏?HUD 寮曠敤锛屽啀瀵圭己澶遍」 fallback 鍚嶅瓧鏌ユ壘鈥濈殑杩囨浮妯″紡銆?

濡傛灉浣犳兂鏀癸細
- 椤堕儴璧勬簮鍖烘牱寮?- 鍙充晶鍙戠數鏈?鐐鍗＄墖鎺掔増
- 鎷栨嫿鎻愮ず鏂囨鍜屼綅缃?- 甯搁┗鐘舵€佹潯瀵硅薄宸蹭粠 HUD 灞傜骇涓墿鐞嗗垹闄?灏变紭鍏堢湅杩欓噷銆?
### `TowerShopCard.cs`
杩欐槸鍙充晶閮ㄧ讲鍗′氦浜掑叆鍙ｃ€?瀹冭礋璐ｏ細
- 鐐瑰嚮鍗＄墖
- 寮€濮嬫嫋鎷?- 鎷栨嫿涓浆鍙戦紶鏍囦綅缃?- 缁撴潫鎷栨嫿
- 鎮仠鍙嶉

瀹冩湰韬笉鍒ゆ柇鑳戒笉鑳藉缓锛屽彧鏄?UI 杈撳叆鍏ュ彛銆?
杩欎竴杞户缁敹灏惧悗锛屽畠涔熶笉鍐嶆牴鎹璞″悕鎺ㄦ柇濉旂被鍨嬶紱閮ㄧ讲鍗¤韩浠界幇鍦ㄥ繀椤荤敱 Inspector 鏄惧紡閰嶇疆銆?
## 4. 鏀剧疆绯荤粺鐩稿叧鑴氭湰
### `BuildZone.cs`
澶ц寖鍥村缓閫犺鍙尯銆?鍙洖绛斺€滆繖涓偣鏈夋病鏈夊湪鍏冲崱鍏佽寤洪€犵殑澶у尯鍩熼噷鈥濄€?
### `PlacementBlocker.cs`
绂佸缓鏍囪銆?璺緞銆佸嚭鐢熺偣銆佸熀鍦版牳蹇冭繖浜涘尯鍩熶細鎸傚畠銆?瀹冨洖绛斺€滆櫧鐒跺湪澶у缓閫犲尯閲岋紝浣嗚繖閲屼緷鐒朵笉鑳藉缓鈥濄€?
### `BuildPad.cs`
鏃у浐瀹氬浣嶇郴缁熺殑鍏煎灞傘€?褰撳墠涓荤帺娉曞凡缁忎笉鐢ㄥ畠浜嗭紝浣嗗畠杩樹繚鐣欑潃鏃ф柟妗堢殑鍗犱綅閫昏緫鍜屾ˉ鎺ヤ唬鐮併€?鐜板湪鏇村鏄巻鍙插吋瀹瑰拰鍙傝€冨璞°€?
## 5. 鎴樻枟鐩稿叧鑴氭湰
### `DefenseTower.cs`
鍩虹鏀诲嚮濉斻€?瀹氭湡鎵弿鏈€杩戞晫浜哄苟鐩存帴鍛戒腑銆?
### `RelayTower.cs`
鍙戠數鏈恒€?鍛ㄦ湡鎬х粰鐜╁澧炲姞鐢甸噺锛屼笉鏀诲嚮鏁屼汉銆?
### `Enemy.cs`
褰撳墠瀹冪殑琛€鏉￠摼璺篃宸茬粡浠庘€滄寜瀛愯妭鐐瑰悕鏌ユ壘鈥濊縼鍒扳€淓nemyPrototype 鏄惧紡搴忓垪鍖?Root / Fill / Background 寮曠敤鈥濓紝鍚庣画鏀硅鏉″瓙鑺傜偣鍚嶆椂鏇村畨鍏ㄣ€?鏁屼汉鏈綋銆?璐熻矗锛?- 娌胯矾寰勭Щ鍔?- 鍙椾激/姝讳骸
- 鎶佃揪鍩哄湴鎵ｈ
- 琛€鏉℃樉绀?
### `EnemyPath.cs`
璺緞鐐瑰鍣ㄣ€?璐熻矗淇濆瓨鏁屼汉璺緞鍜屽嚭鐢熺偣淇℃伅銆?
### `WaveSpawner.cs`
娉㈡鐘舵€佹満銆?璐熻矗鍒锋€妭濂忋€佹瘡娉㈡暟閲忋€佹尝闂撮棿闅旓紝浠ュ強閫氬叧鏉′欢鍒ゆ柇銆?
## 6. 鍘熷瀷鏈熻緟鍔╄剼鏈?### `SceneObjectFinder.cs`
杩欐槸鍘熷瀷鏈熸寜瀵硅薄鍚嶆煡鎵惧満鏅璞＄殑宸ュ叿銆?鍥犱负椤圭洰閲屼粛鏈変竴閮ㄥ垎瀵硅薄寮曠敤涓嶆槸 Inspector 鐩存嫋锛岃€屾槸閫氳繃鍚嶅瓧鍘绘壘锛屾墍浠ュ畠鐜板湪杩樺緢閲嶈銆?

褰撳墠瀹冧粛鐒跺瓨鍦紝浣嗙幇鍦ㄦ洿閫傚悎浣滀负杩囨浮鏈?fallback锛岃€屼笉鍐嶆槸鍚庣画闀挎湡鏋舵瀯鐨勯閫変緷璧栨柟寮忋€?
杩欎篃鎰忓懗鐫€涓€涓」鐩害鏉燂細
鏈変簺瀵硅薄鍚嶄笉鑳介殢渚挎敼銆?
## 7. UI 鍜岃祫婧愭枃浠?### 鍥炬爣涓庨瑙堣祫婧?- `Assets/UI/Icons/relay-card-icon.png`锛氬彂鐢垫満鍗″浘鏍?- `Assets/UI/Icons/defense-card-icon.png`锛氱偖濉斿崱鍥炬爣
- `Assets/Resources/UI/placement-ring.png`锛氬湴闈㈡斁缃瑙堝湀

### TMP 璧勬簮
`Assets/TextMesh Pro/**` 鏄?TextMeshPro 鐨勫瓧浣撳拰榛樿璧勬簮锛屼富瑕佽礋璐ｆ枃鏈覆鏌撳熀纭€璁炬柦銆?
## 8. 閰嶇疆鏂囦欢
### `Packages/manifest.json`
Unity 鍖呬緷璧栨竻鍗曘€?
### `Packages/packages-lock.json`
鍖呯増鏈攣瀹氭枃浠躲€?
### `ProjectSettings/ProjectVersion.txt`
Unity 鐗堟湰閿佸畾锛岀洰鍓嶉」鐩娇鐢?`2022.3.62f3c1`銆?
### `ProjectSettings/EditorBuildSettings.asset`
鏋勫缓鍦烘櫙椤哄簭銆?褰撳墠椤哄簭鏄細
1. `MainMenu`
2. `SampleScene`

## 9. AI 鍗忎綔鏂囨。涓庡伐鍏?### `AGENTS.md`
椤圭洰鍏ュ彛瑙勫垯銆?瀹冭瀹?AI 杩涘叆杩欎釜椤圭洰鍚庤鍏堣鍝簺鏂囨。銆佷粈涔堟椂鍊欐洿鏂拌蹇嗐€佽剼鏈敞閲婅淇濇寔浠€涔堥鏍笺€?
### `docs/ai-memory/td-memory-main.md`
涓昏蹇嗘枃妗ｏ紝璁板綍椤圭洰姒傚喌銆佹牳蹇冭鍒欍€佹枃浠剁储寮曞拰娴佺▼銆?
### `docs/ai-memory/td-memory-architecture.md`
鏋舵瀯鏂囨。锛岃褰曞満鏅閰嶃€丠UD 缁撴瀯銆佹斁缃摼璺€?
### `docs/ai-memory/td-memory-rules-and-history.md`
瑙勮寖銆佸巻鍙插拰璺嚎鍥炬枃妗ｃ€?
### `docs/ai-memory/td-agent-development-playbook.md`
杩欐槸缁欐湭鏉ョ淮鎶よ€呭拰鏅鸿兘浣撴帴鎵嬪紑鍙戞椂浣跨敤鐨勯暱鏈熸墽琛屾墜鍐屻€?瀹冧笉鏄崟绾褰曗€滃綋鍓嶉」鐩槸浠€涔堟牱鈥濓紝鑰屾槸璇︾粏璇存槑锛?- 搴旇鎸変粈涔堥樁娈电户缁紑鍙?- 濉旈槻鍏冲崱濡備綍婕旇繘鎴愬鍏冲崱缁撴瀯
- 2D 妯澘鍓ф儏鍦烘櫙璇ュ浣曟帴杩涙潵
- 涓昏彍鍗曘€佸墽鎯呭満鏅€佸闃插叧鍗′箣闂寸殑鍒囨崲搴旇鎬庢牱瀹炵幇

### `docs/ai-memory/td-project-file-guide.md`
鎸佺画缁存姢鐢ㄧ殑鏂囦欢鎸囧崡銆?瀹冩洿鍋忊€滅储寮曡〃鈥濆拰鈥滅淮鎶よ鍒欌€濄€?
### `docs/ai-memory/skills/maintain-project-file-guide/SKILL.md`
鏈湴 skill銆?瀹冭瀹氫簡锛?- 鏂囦欢缁撴瀯鍙樺寲鍚庯紝蹇呴』鏇存柊鍝簺鏂囨。
- 杩欑被鏂囨。搴旇鎬庝箞缁存姢

## 10. 濡傛灉浣犳兂鏀规煇绫诲唴瀹癸紝鍏堢湅鍝噷
### 鏀逛富鑿滃崟
鍏堢湅锛?- `Assets/Scenes/MainMenu.unity`
- `Assets/Scripts/TowerDefense/MainMenuController.cs`
- `ProjectSettings/EditorBuildSettings.asset`

### 鏀规斁缃鍒?鍏堢湅锛?- `Assets/Scripts/TowerDefense/TowerDefenseGame.cs`
- `Assets/Scripts/TowerDefense/TowerCatalog.cs`
- `Assets/Scripts/TowerDefense/BuildZone.cs`
- `Assets/Scripts/TowerDefense/PlacementBlocker.cs`
- `Assets/Scripts/TowerDefense/TowerShopCard.cs`

### 鏀?HUD / 鍙充晶閮ㄧ讲鍖?鍏堢湅锛?- `Assets/Scripts/TowerDefense/TowerDefenseHudPresenter.cs`
- `Assets/Scenes/SampleScene.unity`
- `Assets/UI/Icons/*`

### 鏀规垬鏂?鍏堢湅锛?- `Assets/Scripts/TowerDefense/DefenseTower.cs`
- `Assets/Scripts/TowerDefense/RelayTower.cs`
- `Assets/Scripts/TowerDefense/Enemy.cs`
- `Assets/Scripts/TowerDefense/WaveSpawner.cs`
- `Assets/Scripts/TowerDefense/EnemyPath.cs`

## 11. 杩欎釜鏂囨。鎬庝箞鐢?浣犱互鍚庡鏋滃彧鏄兂蹇€熷垽鏂€滄煇涓笢瑗胯鍘诲摢閲屾敼鈥濓紝浼樺厛鐪嬭繖浠芥枃妗ｃ€?濡傛灉浣犳兂鐪嬫洿鍍忕储寮曡〃銆佸苟涓斿甫缁存姢绾︽潫鐨勭増鏈紝灏辩湅锛?- `docs/ai-memory/td-project-file-guide.md`

濡傛灉浣犳兂缁х画鎵╄繖涓」鐩紝灏ゅ叾鏄兂鍔犲叆鏇村濉旈槻鍏冲崱銆佹í鏉垮墽鎯呭満鏅紝鎴栬€呮兂鎼炴竻妤氬悗缁帹鑽愮殑寮€鍙戦『搴忥紝灏变紭鍏堝啀鐪嬶細
- `docs/ai-memory/td-agent-development-playbook.md`

濡傛灉浣犲彂鐜帮細
- 鏂板浜嗗叧閿剼鏈?- 鏂板浜嗗叧閿満鏅?- 鍦烘櫙鍏ュ彛鍙樹簡
- 鏌愪釜鑴氭湰鑱岃矗鏄庢樉鍙樺寲浜?閭ｈ鏄庤繖浠芥枃妗ｅ拰鏂囦欢鎸囧崡閮藉簲璇ユ洿鏂般€?

## 12. 2026-04-10 结构补充

- `Assets/Scripts/TowerDefense/TowerPlacementVisualController.cs`
  这是这轮新拆出的“放置可视化协调器”。它统一管理预览塔、精确合法区覆盖层和首塔起手区标记，让 `TowerDefenseGame` 不再自己塞满这三块实现细节。
- `Assets/Scripts/TowerDefense/StarterZoneMarkerRenderer.cs`
  只负责把首塔起手区画成世界空间方形标记。它不做任何规则判定，所以以后改样式可以优先看这里。
- `Assets/Scripts/TowerDefense/PlacementAreaOverlayRenderer.cs`
  只负责把 `ValidatePlacementPosition` 的结果采样成覆盖层纹理。它不直接知道 BuildZone、PlacementBlocker 或塔间距规则，只消费外部传进来的 validator。
- 当前职责边界可以这样记：
  - “能不能放”继续看 `TowerDefenseGame.cs`
  - “拖拽时看见什么”优先看这 3 个新文件

## 13. 2026-04-11 结构补充

- `Assets/Scripts/TowerDefense/TowerPlacementRules.cs`
  这是第二轮组件化新增的放置规则组件。起手区、扩张部署网络、阻挡器、塔间距和覆盖层扫描边界现在都优先从这里看。
- 当前职责边界更新为：
  - “能不能放”优先看 `TowerPlacementRules.cs`
  - “拖拽时看见什么”优先看 `TowerPlacementVisualController.cs`
  - `TowerDefenseGame.cs` 继续负责总协调、资源扣减、真正建塔和 HUD 刷新入口
