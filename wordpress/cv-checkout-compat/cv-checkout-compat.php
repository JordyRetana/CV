<?php
/**
 * Plugin Name: CV Checkout Compatibility
 * Description: Keeps public checkout requests working with UCMM and sends CV Editor carts to their checkout.
 * Version: 1.1.0
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

add_filter(
	'woocommerce_get_checkout_url',
	static function ( $checkout_url ) {
		if ( ! function_exists( 'is_cart' ) || ! is_cart() || ! function_exists( 'WC' ) || ! WC()->cart ) {
			return $checkout_url;
		}

		foreach ( WC()->cart->get_cart() as $item ) {
			if ( 1311 === (int) ( $item['product_id'] ?? 0 ) ) {
				return home_url( '/comprar-cv-desktop-editor/' );
			}
		}

		return $checkout_url;
	},
	20
);
