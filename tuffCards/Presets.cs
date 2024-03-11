namespace TuffCards;

public static class Presets {
	public const string DefaultWrapper = """
<div class="wrapper {{ name }}">{{ for card in cards }}
	{{ card }}{{ end }}
</div>

<style>
	.wrapper {
		display: flex;
		flex-wrap: wrap;
		gap: 8px;
	}

	.actions > div {
		height: 500px;
		width: 200px;
		display: flex;
		flex-direction: column;
		align-items: stretch;
		border-radius: 8px;
		border: solid thin black;
		background: linear-gradient(to bottom right, #220088, #880022);
		color: white;
		position: relative;

		> .cost {
			flex: 0 0 auto;
			align-self: stretch;
			background: rgba(0, 0, 0, 0.5);
		}
		> .effect {
			flex: 1 0 auto;
			display: flex;
			flex-direction: column;
			justify-content: center;
			align-items: center;
			margin: 8px;
		}
		.image {
			width: 184px;
			margin: 8px;
			flex: 1 0 auto;
		}
	}

	.icon {
		height: 16px;
	}

	p {
		margin: 0;
	}
</style>
""";

	public const string DefaultActions = """
<div>
	<div class="cost">{{ Cost }}</div>
	{{ Image }}
	<div class="effect">
		<div>{{ Effect }}</div>
	</div>
</div>
""";

	public const string DefaultActionsData = """
Cost;Effect;Image
1;Do something;
2;'tap: Do something *really* **strong**;"strong
""";

	public const string TapImage = """<svg style="height: 512px; width: 512px;" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><g class="" style="" transform="translate(0,0)"><path d="M263.09 50c-11.882-.007-23.875 1.018-35.857 3.13C142.026 68.156 75.156 135.026 60.13 220.233 45.108 305.44 85.075 391.15 160.005 434.41c32.782 18.927 69.254 27.996 105.463 27.553 46.555-.57 92.675-16.865 129.957-48.15l-30.855-36.768c-50.95 42.75-122.968 49.05-180.566 15.797-57.597-33.254-88.152-98.777-76.603-164.274 11.55-65.497 62.672-116.62 128.17-128.168 51.656-9.108 103.323 7.98 139.17 43.862L327 192h128V64l-46.34 46.342C370.242 71.962 317.83 50.03 263.09 50z" fill="#fff" fill-opacity="1"></path></g></svg>""";
	public const string StrongImage = """<svg style="height: 512px; width: 512px;" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><g class="" style="" transform="translate(0,0)"><path d="M257.375 20.313c-13.418 0-26.07 7.685-35.938 21.75-9.868 14.064-16.343 34.268-16.343 56.75 0 22.48 6.475 42.654 16.344 56.718 9.868 14.066 22.52 21.75 35.937 21.75 13.418 0 26.038-7.684 35.906-21.75 9.87-14.063 16.376-34.236 16.376-56.718 0-22.48-6.506-42.685-16.375-56.75-9.867-14.064-22.487-21.75-35.905-21.75zm-150.25 43.062c-20.305.574-23.996 13.892-31.78 29.03-23.298 45.304-55.564 164.75-55.564 164.75l160.47-5.436 29.125 137.593-22.78 106.03h149.093l-22.282-106 24.25-137.5 157.53 5.313c.002 0-32.264-119.447-55.56-164.75-7.787-15.14-11.477-28.457-31.782-29.03-17.898 0-32.406 15.552-32.406 34.718 0 19.166 14.508 34.72 32.406 34.72 3.728 0 7.258-.884 10.594-2.126l7.937 74.406L309.437 165c-.285.42-.552.867-.843 1.28-12.436 17.724-30.604 29.69-51.22 29.69-20.614 0-38.782-11.966-51.218-29.69-.277-.395-.54-.816-.812-1.218l-116.75 40.032 7.937-74.406c3.337 1.242 6.867 2.125 10.595 2.125 17.898 0 32.406-15.553 32.406-34.72 0-19.165-14.507-34.718-32.405-34.718z" fill="#fff" fill-opacity="1"></path></g></svg>""";
}