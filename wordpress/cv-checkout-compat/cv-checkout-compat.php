<?php
/**
 * Plugin Name: CV Checkout Compatibility
 * Description: Lets WooCommerce checkout requests from public checkout pages run while UCMM keeps the rest of the site hidden.
 * Version: 1.0.0
 * Author: Jordy Retana
 */

if ( ! defined( 'ABSPATH' ) ) {
	exit;
}

add_action(
	'wp',
	static function () {
		if ( ! isset( $_GET['wc-ajax'] ) || ! class_exists( 'UCMM_WPBrigade' ) ) {
			return;
		}

		$endpoint = sanitize_key( wp_unslash( $_GET['wc-ajax'] ) );
		if ( ! preg_match( '/\A[a-z0-9_-]{1,80}\z/D', $endpoint ) ) {
			return;
		}

		$referer = wp_get_referer();
		if ( ! $referer || wp_parse_url( $referer, PHP_URL_HOST ) !== wp_parse_url( home_url(), PHP_URL_HOST ) ) {
			return;
		}

		$path = untrailingslashit( wp_parse_url( $referer, PHP_URL_PATH ) );
		if ( ! in_array( $path, array( '/comprar-cv-desktop-editor', '/finalizar-compra', '/carrito' ), true ) ) {
			return;
		}

		global $wp_filter;
		if ( ! isset( $wp_filter['wp'] ) ) {
			return;
		}

		foreach ( $wp_filter['wp']->callbacks[10] ?? array() as $callback ) {
			$handler = $callback['function'] ?? null;
			if ( is_array( $handler ) && $handler[0] instanceof UCMM_WPBrigade && 'ucmm_parse_request' === $handler[1] ) {
				remove_action( 'wp', $handler, 10 );
			}
		}
	},
	1
);
